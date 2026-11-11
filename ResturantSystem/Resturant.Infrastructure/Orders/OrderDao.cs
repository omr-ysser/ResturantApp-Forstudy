using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Common;
using Resturant.Domain.Entities;
using Resturant.Domain.Enums;

namespace Resturant.Infrastructure.Orders;

public sealed class OrderDao(
    IAppDbContext dbContext)
    : IOrderDao
{
    public async Task<PagedResponse<OrderListItemResponse>> ListAsync(
        OrderListFilter filter,
        Guid? customerScopeUserId,
        CancellationToken cancellationToken)
    {
        var orders = dbContext.Orders.AsNoTracking();

        if (customerScopeUserId.HasValue)
        {
            var ownerId = customerScopeUserId.Value;

            orders = orders.Where(order =>
                order.CustomerUserId == ownerId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();

            orders = orders.Where(order =>
                order.OrderNumber.Contains(search));
        }

        if (filter.Status.HasValue)
        {
            var status = filter.Status.Value;
            orders = orders.Where(order => order.Status == status);
        }

        if (filter.Type.HasValue)
        {
            var type = filter.Type.Value;
            orders = orders.Where(order => order.Type == type);
        }

        if (filter.FromUtc.HasValue)
        {
            var from = filter.FromUtc.Value.ToUniversalTime();

            orders = orders.Where(order =>
                order.CreatedAtUtc >= from);
        }

        if (filter.ToUtc.HasValue)
        {
            var to = filter.ToUtc.Value.ToUniversalTime();

            orders = orders.Where(order =>
                order.CreatedAtUtc < to);
        }

        var query = orders.Select(order => new
        {
            order.Id,
            order.OrderNumber,
            order.Type,
            order.Status,
            order.CustomerUserId,
            order.DiningTableId,
            order.CreatedAtUtc,

            LineCount = order.Items.Count,

            TotalAmount = order.Items
                .Sum(item =>
                    (decimal?)(item.UnitPrice * item.Quantity))
                ?? 0m
        });

        if (filter.MinTotal.HasValue)
        {
            var minimum = filter.MinTotal.Value;

            query = query.Where(order =>
                order.TotalAmount >= minimum);
        }

        if (filter.MaxTotal.HasValue)
        {
            var maximum = filter.MaxTotal.Value;

            query = query.Where(order =>
                order.TotalAmount <= maximum);
        }

        var count = await query.CountAsync(cancellationToken);

        var ordered = filter.SortBy.ToLowerInvariant() switch
        {
            "createdat" => filter.Descending
                ? query.OrderByDescending(order => order.CreatedAtUtc)
                : query.OrderBy(order => order.CreatedAtUtc),

            "ordernumber" => filter.Descending
                ? query.OrderByDescending(order => order.OrderNumber)
                : query.OrderBy(order => order.OrderNumber),

            "total" => filter.Descending
                ? query.OrderByDescending(order => order.TotalAmount)
                : query.OrderBy(order => order.TotalAmount),

            _ => throw new ArgumentOutOfRangeException(
                nameof(filter.SortBy),
                "Unsupported order sorting field.")
        };

        var rows = await ordered
            .ThenBy(order => order.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(order => new OrderListItemResponse(
            order.Id,
            order.OrderNumber,
            order.Type.ToString(),
            order.Status.ToString(),
            order.CustomerUserId,
            order.DiningTableId,
            order.LineCount,
            order.TotalAmount,
            OrderRules.Currency,
            order.CreatedAtUtc))
            .ToArray();

        return new PagedResponse<OrderListItemResponse>(
            filter.Page,
            filter.PageSize,
            count,
            items);
    }

    public Task<OrderReceiptLookup?> FindReceiptAsync(
        Guid userId,
        Guid requestKey,
        CancellationToken cancellationToken)
    {
        return dbContext.Orders
            .AsNoTracking()
            .Where(order =>
                order.CreationReceipt != null &&
                order.CreationReceipt.UserId == userId &&
                order.CreationReceipt.RequestKey == requestKey)
            .Select(order => new OrderReceiptLookup(
                order.Id,
                order.CreationReceipt!.RequestHash))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrderDishReference>>
        GetDishReferencesAsync(
            Guid[] menuItemIds,
            CancellationToken cancellationToken)
    {
        return await dbContext.MenuItems
            .AsNoTracking()
            .Where(item => menuItemIds.Contains(item.Id))
            .Select(item => new OrderDishReference(
                item.Id,
                item.CategoryId))
            .ToListAsync(cancellationToken);
    }

    public Task<MenuItem?> LockDishAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return dbContext.MenuItems
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [dbo].[MenuItems] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {id}
                """)
            .AsTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<DiningTable?> LockTableAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return dbContext.DiningTables
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [dbo].[DiningTables] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {id}
                """)
            .AsTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasActiveTableOrderAsync(
        Guid tableId,
        CancellationToken cancellationToken)
    {
        return dbContext.Orders
            .AsNoTracking()
            .AnyAsync(order =>
                    order.DiningTableId == tableId &&
                    order.Status != OrderStatus.Completed &&
                    order.Status != OrderStatus.Cancelled,
                cancellationToken);
    }

    public Task<bool> HasSeatedReservationAsync(
        Guid tableId,
        CancellationToken cancellationToken)
    {
        return dbContext.Reservations
            .AsNoTracking()
            .AnyAsync(reservation =>
                    reservation.DiningTableId == tableId &&
                    reservation.Status == ReservationStatus.Seated,
                cancellationToken);
    }

    public async Task<OrderResponse?> GetAsync(
        Guid id,
        Guid? customerScopeUserId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Orders
            .AsNoTracking()
            .Where(order => order.Id == id);

        if (customerScopeUserId.HasValue)
        {
            var ownerId = customerScopeUserId.Value;

            query = query.Where(order =>
                order.CustomerUserId == ownerId);
        }

        var row = await query
            .Select(order => new
            {
                order.Id,
                order.OrderNumber,
                order.Type,
                order.Status,
                order.CustomerUserId,
                order.DiningTableId,
                order.ReservationId,
                order.CustomerName,
                order.CustomerPhone,
                order.DeliveryAddress,
                order.Notes,
                order.CreatedAtUtc,

                Version = EF.Property<byte[]>(
                    order,
                    "RowVersion"),

                Items = order.Items
                    .OrderBy(item => item.CreatedAtUtc)
                    .ThenBy(item => item.Id)
                    .Select(item => new OrderItemResponse(
                        item.Id,
                        item.MenuItemId,
                        item.ItemName,
                        item.UnitPrice,
                        item.Quantity,
                        item.UnitPrice * item.Quantity,
                        item.Notes))
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        return new OrderResponse(
            row.Id,
            row.OrderNumber,
            row.Type.ToString(),
            row.Status.ToString(),
            row.CustomerUserId,
            row.DiningTableId,
            row.CustomerName,
            row.CustomerPhone,
            row.DeliveryAddress,
            row.Notes,
            OrderRules.Currency,
            row.Items.Sum(item => item.TotalPrice),
            row.CreatedAtUtc,
            Convert.ToBase64String(row.Version),
            row.Items,
            row.ReservationId);
    }

    public void Add(Order order)
    {
        order.EnsureReadyForPersistence();

        if (order.CreationReceipt is null)
        {
            throw new InvalidOperationException(
                "New orders require an idempotency receipt.");
        }

        if (order.StatusHistory.Count == 0)
        {
            throw new InvalidOperationException(
                "New orders require an initial status history entry.");
        }

        dbContext.Orders.Add(order);
    }
}