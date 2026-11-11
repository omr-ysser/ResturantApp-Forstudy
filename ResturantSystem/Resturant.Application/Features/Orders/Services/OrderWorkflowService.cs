using Microsoft.Extensions.Logging;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Common.Security;
using Resturant.Domain.Entities;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.Orders.Services;

public sealed class OrderWorkflowService(
    IOrderDao orderDao,
    IOrderWorkflowDao workflowDao,
    IAuthenticationDao authenticationDao,
    IAuthenticationSessionDao sessionDao,
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<OrderWorkflowService> logger)
    : IOrderWorkflowService
{
    public Task<OrderResponse> ChangeStatusAsync(
        Guid id,
        OrderStatus status,
        string version,
        CancellationToken cancellationToken)
    {
        return ExecuteAsync(
            id,
            status,
            reason: null,
            version,
            cancellationToken);
    }

    public Task<OrderResponse> CancelAsync(
        Guid id,
        string reason,
        string version,
        CancellationToken cancellationToken)
    {
        return ExecuteAsync(
            id,
            OrderStatus.Cancelled,
            reason,
            version,
            cancellationToken);
    }

    private async Task<OrderResponse> ExecuteAsync(
        Guid id,
        OrderStatus targetStatus,
        string? reason,
        string version,
        CancellationToken cancellationToken)
    {
        if (!RowVersionFormat.IsValid(version))
        {
            throw new BusinessConflictException(
                "The order version is invalid. Reload the order.");
        }

        var expectedVersion = Convert.FromBase64String(version);
        var actorId = currentUser.UserId;

        await using var transaction =
            await dbContext.BeginTransactionAsync(cancellationToken);

        await sessionDao.LockUserAsync(actorId, cancellationToken);

        var access = await authenticationDao.GetAccessAsync(
            actorId,
            cancellationToken);

        if (access is null ||
            !await sessionDao.IsSessionActiveAsync(
                actorId,
                currentUser.SessionId,
                timeProvider.GetUtcNow(),
                cancellationToken))
        {
            throw new AuthenticationFailedException();
        }

        var isCancellation = targetStatus == OrderStatus.Cancelled;

        var canCancelAll = access.Permissions.Contains(
            "Orders.CancelAll",
            StringComparer.Ordinal);

        var canCancelOwn = access.Permissions.Contains(
            "Orders.CancelOwn",
            StringComparer.Ordinal);

        var canChangeStatus = access.Permissions.Contains(
            "Orders.ChangeStatus",
            StringComparer.Ordinal);

        if (isCancellation)
        {
            if (!canCancelAll && !canCancelOwn)
            {
                throw new ForbiddenOperationException();
            }
        }
        else if (!canChangeStatus)
        {
            throw new ForbiddenOperationException();
        }

        Guid? customerScope =
            isCancellation && !canCancelAll ? actorId : null;

        var snapshot = await orderDao.GetAsync(
            id,
            customerScope,
            cancellationToken)
            ?? throw new ResourceNotFoundException("Order");

        DiningTable? table = null;

        if (snapshot.DiningTableId.HasValue)
        {
            table = await orderDao.LockTableAsync(
                snapshot.DiningTableId.Value,
                cancellationToken)
                ?? throw new ResourceNotFoundException("Dining table");
        }

        var order = await workflowDao.GetTrackedForUpdateAsync(
            id,
            cancellationToken)
            ?? throw new ResourceNotFoundException("Order");

        if (customerScope.HasValue &&
            order.CustomerUserId != customerScope.Value)
        {
            throw new ResourceNotFoundException("Order");
        }

        if (order.DiningTableId != snapshot.DiningTableId)
        {
            throw new BusinessConflictException(
                "The order table changed. Reload the order.");
        }

        if (!workflowDao.GetVersion(order)
                .AsSpan()
                .SequenceEqual(expectedVersion))
        {
            throw new BusinessConflictException(
                "The order has changed. Reload it and try again.");
        }

        EnsureAllowedTransition(order.Status, targetStatus);

        if (isCancellation &&
            await workflowDao.HasPaidPaymentsAsync(
                order.Id,
                cancellationToken))
        {
            throw new BusinessConflictException(
                "The order has a paid payment. " +
                "Refund processing is required before cancellation.");
        }

        workflowDao.SetExpectedVersion(order, expectedVersion);

        var now = timeProvider.GetUtcNow();

        switch (targetStatus)
        {
            case OrderStatus.Confirmed:
                order.Confirm(actorId, now);
                break;

            case OrderStatus.Preparing:
                order.StartPreparing(actorId, now);
                break;

            case OrderStatus.Ready:
                order.MarkReady(actorId, now);
                break;

            case OrderStatus.Completed:
                order.Complete(actorId, now);
                break;

            case OrderStatus.Cancelled:
                order.Cancel(reason!, actorId, now);
                break;

            default:
                throw new BusinessConflictException(
                    "The requested order status is not supported.");
        }

        var isTerminal = targetStatus is
            OrderStatus.Completed or OrderStatus.Cancelled;

        if (isTerminal &&
            table is not null &&
            table.Status == TableStatus.Occupied)
        {
            var hasOtherOrders =
                await workflowDao.HasOtherActiveTableOrdersAsync(
                    table.Id,
                    order.Id,
                    cancellationToken);

            var hasSeatedReservation =
                await orderDao.HasSeatedReservationAsync(
                    table.Id,
                    cancellationToken);

            if (!hasOtherOrders && !hasSeatedReservation)
            {
                table.Release();
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await orderDao.GetAsync(
            order.Id,
            customerScope,
            cancellationToken)
            ?? throw new ResourceNotFoundException("Order");

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Order {OrderId} changed from {PreviousStatus} to {NewStatus} by user {UserId}",
            order.Id,
            snapshot.Status,
            targetStatus,
            actorId);

        return response;
    }

    private static void EnsureAllowedTransition(
        OrderStatus currentStatus,
        OrderStatus targetStatus)
    {
        var allowed = (currentStatus, targetStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Confirmed) => true,
            (OrderStatus.Confirmed, OrderStatus.Preparing) => true,
            (OrderStatus.Preparing, OrderStatus.Ready) => true,
            (OrderStatus.Ready, OrderStatus.Completed) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
            _ => false
        };

        if (!allowed)
        {
            throw new BusinessConflictException(
                $"Cannot change order from {currentStatus} to {targetStatus}.");
        }
    }
}