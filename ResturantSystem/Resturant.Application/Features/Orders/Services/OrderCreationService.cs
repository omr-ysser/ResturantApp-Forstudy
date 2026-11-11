using Microsoft.Extensions.Logging;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Orders.Create;
using Restaurant.Application.Features.Reservations;
using Resturant.Domain.Common;
using Resturant.Domain.Entities;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.Orders.Services;

public sealed class OrderCreationService(
    IOrderDao orderDao,
    ICategoryDao categoryDao,
    ICustomerProfileDao customerProfileDao,
    IReservationDao reservationDao,
    IAuthenticationDao authenticationDao,
    IAuthenticationSessionDao sessionDao,
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<OrderCreationService> logger)
    : IOrderCreationService
{
    public async Task<OrderCreationResult> CreateAsync(
        Guid requestKey,
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var requestHash = OrderRequestFingerprint.Create(request);

        await using var transaction =
            await dbContext.BeginTransactionAsync(cancellationToken);

        await sessionDao.LockUserAsync(userId, cancellationToken);

        var access = await authenticationDao.GetAccessAsync(
            userId,
            cancellationToken);

        if (access is null ||
            !await sessionDao.IsSessionActiveAsync(
                userId,
                currentUser.SessionId,
                timeProvider.GetUtcNow(),
                cancellationToken))
        {
            throw new AuthenticationFailedException();
        }

        if (!access.Permissions.Contains(
                "Orders.Create",
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        if (request.Type == OrderType.DineIn &&
            !access.Permissions.Contains(
                "Orders.ChangeStatus",
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        if (request.ReservationId.HasValue)
        {
            if (request.Type != OrderType.DineIn)
            {
                throw new BusinessConflictException(
                    "Only dine-in orders can use a reservation.");
            }

            if (!access.Permissions.Contains(
                    ReservationPermissions.Manage,
                    StringComparer.Ordinal))
            {
                throw new ForbiddenOperationException();
            }
        }

        var receipt = await orderDao.FindReceiptAsync(
            userId,
            requestKey,
            cancellationToken);

        if (receipt is not null)
        {
            if (!string.Equals(
                    receipt.RequestHash,
                    requestHash,
                    StringComparison.Ordinal))
            {
                throw new BusinessConflictException(
                    "The idempotency key was already used with a different request.");
            }

            // The receipt lookup is scoped to the authenticated creator.
            // The customer owner may be different for reservation orders.
            var existing = await GetCreatedOrderResponseAsync(
                receipt.OrderId,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Order {OrderId} replayed for user {UserId}",
                existing.Id,
                userId);

            return new OrderCreationResult(existing, true);
        }

        Guid? customerUserId = userId;

        string? customerName = request.CustomerName;
        string? customerPhone = request.CustomerPhone;
        string? deliveryAddress = request.DeliveryAddress;
        string? notes = request.Notes;

        if (request.DeliveryAddressId.HasValue)
        {
            if (request.Type != OrderType.Delivery)
            {
                throw new BusinessConflictException(
                    "Saved addresses are supported only for delivery orders.");
            }

            var profile =
                await customerProfileDao
                    .GetTrackedWithAddressesForUserAsync(
                        userId,
                        cancellationToken);

            var address = profile?.DeliveryAddresses
                .SingleOrDefault(item =>
                    item.Id == request.DeliveryAddressId.Value);

            if (address is null)
            {
                throw new ResourceNotFoundException("Delivery address");
            }

            customerName = address.RecipientName;
            customerPhone = address.RecipientPhone;
            deliveryAddress = address.ToOrderAddressSnapshot();

            notes = BuildOrderNotes(
                request.Notes,
                address.DeliveryInstructions);
        }

        var menuItemIds = request.Items
            .Select(item => item.MenuItemId)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        var references = await orderDao.GetDishReferencesAsync(
            menuItemIds,
            cancellationToken);

        if (references.Count != menuItemIds.Length)
        {
            throw new ResourceNotFoundException("Dish");
        }

        var referenceById = references.ToDictionary(item => item.Id);

        var categoryIds = references
            .Select(item => item.CategoryId)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        foreach (var categoryId in categoryIds)
        {
            var category = await categoryDao.LockForUpdateAsync(
                categoryId,
                cancellationToken)
                ?? throw new ResourceNotFoundException("Category");

            if (!category.IsActive)
            {
                throw new BusinessConflictException(
                    "One or more dishes belong to an inactive category.");
            }
        }

        var dishes = new Dictionary<Guid, MenuItem>();

        foreach (var menuItemId in menuItemIds)
        {
            var dish = await orderDao.LockDishAsync(
                menuItemId,
                cancellationToken)
                ?? throw new ResourceNotFoundException("Dish");

            if (dish.CategoryId != referenceById[menuItemId].CategoryId)
            {
                throw new BusinessConflictException(
                    "A dish category changed. Retry with a new request.");
            }

            if (!dish.IsAvailable)
            {
                throw new BusinessConflictException(
                    "One or more dishes are unavailable.");
            }

            dishes.Add(dish.Id, dish);
        }

        DiningTable? table = null;
        Reservation? linkedReservation = null;

        if (request.Type == OrderType.DineIn)
        {
            table = await orderDao.LockTableAsync(
                request.DiningTableId!.Value,
                cancellationToken)
                ?? throw new ResourceNotFoundException("Dining table");

            if (request.ReservationId.HasValue)
            {
                // Keep the same table-before-reservation lock order
                // used by reservation lifecycle actions.
                linkedReservation =
                    await reservationDao.GetTrackedForUpdateAsync(
                        request.ReservationId.Value,
                        cancellationToken)
                    ?? throw new ResourceNotFoundException("Reservation");

                if (linkedReservation.DiningTableId != table.Id)
                {
                    throw new BusinessConflictException(
                        "The reservation belongs to a different dining table.");
                }

                if (linkedReservation.Status != ReservationStatus.Seated)
                {
                    throw new BusinessConflictException(
                        "The reservation must be seated before creating its order.");
                }

                if (table.Status != TableStatus.Occupied)
                {
                    throw new BusinessConflictException(
                        "The seated reservation requires an occupied table.");
                }

                if (await reservationDao.HasOtherSeatedReservationsAsync(
                        table.Id,
                        linkedReservation.Id,
                        cancellationToken))
                {
                    throw new BusinessConflictException(
                        "The table has another seated reservation.");
                }

                customerUserId = linkedReservation.CustomerUserId;
                customerName = linkedReservation.CustomerName;
                customerPhone = linkedReservation.CustomerPhone;
            }
            else
            {
                if (table.Status != TableStatus.Available ||
                    await orderDao.HasSeatedReservationAsync(
                        table.Id,
                        cancellationToken))
                {
                    throw new BusinessConflictException(
                        "The dining table is not available.");
                }
            }

            if (await orderDao.HasActiveTableOrderAsync(
                    table.Id,
                    cancellationToken))
            {
                throw new BusinessConflictException(
                    "The dining table already has an active order.");
            }
        }

        var order = new Order(
            orderNumber: $"ORD-{Guid.NewGuid():N}",
            type: request.Type,
            createdByUserId: userId,
            diningTableId: request.DiningTableId,
            customerName: customerName,
            customerPhone: customerPhone,
            deliveryAddress: deliveryAddress,
            notes: notes,
            customerUserId: customerUserId);

        foreach (var item in request.Items)
        {
            order.AddItem(
                dishes[item.MenuItemId],
                item.Quantity,
                item.Notes);
        }

        if (linkedReservation is not null)
        {
            order.AttachReservation(linkedReservation);
        }

        order.EnsureReadyForPersistence();

        order.RegisterCreationReceipt(requestKey, requestHash);

        // Reservation seating already occupied the table.
        if (table is not null && linkedReservation is null)
        {
            table.Occupy();
        }

        orderDao.Add(order);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetCreatedOrderResponseAsync(
            order.Id,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Order {OrderId} created by user {UserId} for reservation {ReservationId}",
            order.Id,
            userId,
            order.ReservationId);

        return new OrderCreationResult(response, false);
    }

    private async Task<OrderResponse> GetCreatedOrderResponseAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        // Called only with a newly created order ID or an ID obtained
        // from a receipt belonging to the authenticated creator.
        // Public order reads still enforce their own ownership scope.
        return await orderDao.GetAsync(
                   orderId,
                   customerScopeUserId: null,
                   cancellationToken)
               ?? throw new ResourceNotFoundException("Order");
    }

    private static string? BuildOrderNotes(
        string? customerNotes,
        string? deliveryInstructions)
    {
        var notes = Normalize(customerNotes);
        var instructions = Normalize(deliveryInstructions);

        var result = instructions is null
            ? notes
            : notes is null
                ? $"Delivery instructions: {instructions}"
                : $"{notes}\n\nDelivery instructions: {instructions}";

        if (result is not null &&
            result.Length > OrderRules.MaximumNotesLength)
        {
            throw new BusinessConflictException(
                $"Order notes combined with saved delivery instructions " +
                $"must not exceed {OrderRules.MaximumNotesLength} characters.");
        }

        return result;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}