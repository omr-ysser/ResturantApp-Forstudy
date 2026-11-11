using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Common;
using Resturant.Domain.Enums;
using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class OrderStatusHistory : BaseEntity
{
    private OrderStatusHistory()
    {
    }

    internal OrderStatusHistory(
        Guid orderId,
        OrderStatus? fromStatus,
        OrderStatus toStatus,
        Guid actorUserId,
        DateTimeOffset occurredAtUtc,
        string? reason = null)
    {
        if (orderId == Guid.Empty)
        {
            throw new DomainException(
                "Order is required.");
        }

        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(
                "Status change actor is required.");
        }

        if (occurredAtUtc == default)
        {
            throw new DomainException(
                "Status change timestamp is required.");
        }

        if (!IsAllowedTransition(fromStatus, toStatus))
        {
            throw new DomainException(
                "Invalid order status history transition.");
        }

        var normalizedReason = OrderRules.OptionalText(
            reason,
            OrderRules.MaximumCancellationReasonLength,
            "Status change reason");

        if (toStatus == OrderStatus.Cancelled &&
            normalizedReason is null)
        {
            throw new DomainException(
                "Cancellation reason is required.");
        }

        if (toStatus != OrderStatus.Cancelled &&
            normalizedReason is not null)
        {
            throw new DomainException(
                "A reason is only supported for cancellation.");
        }

        OrderId = orderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ActorUserId = actorUserId;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
        Reason = normalizedReason;
    }

    public Guid OrderId { get; private set; }

    public Order Order { get; private set; } = null!;

    public OrderStatus? FromStatus { get; private set; }

    public OrderStatus ToStatus { get; private set; }

    public Guid ActorUserId { get; private set; }

    public User ActorUser { get; private set; } = null!;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string? Reason { get; private set; }

    private static bool IsAllowedTransition(
        OrderStatus? fromStatus,
        OrderStatus toStatus)
    {
        return (fromStatus, toStatus) switch
        {
            (null, OrderStatus.Pending) => true,

            (OrderStatus.Pending,
                OrderStatus.Confirmed) => true,

            (OrderStatus.Confirmed,
                OrderStatus.Preparing) => true,

            (OrderStatus.Preparing,
                OrderStatus.Ready) => true,

            (OrderStatus.Ready,
                OrderStatus.Completed) => true,

            (OrderStatus.Pending,
                OrderStatus.Cancelled) => true,

            (OrderStatus.Confirmed,
                OrderStatus.Cancelled) => true,

            _ => false
        };
    }
}
