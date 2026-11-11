using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Common;
using Resturant.Domain.Enums;
using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class ReservationStatusHistory : BaseEntity
{
    private ReservationStatusHistory()
    {
    }

    internal ReservationStatusHistory(
        Guid reservationId,
        ReservationStatus? fromStatus,
        ReservationStatus toStatus,
        Guid actorUserId,
        DateTimeOffset occurredAtUtc,
        string? reason = null)
    {
        if (reservationId == Guid.Empty)
        {
            throw new DomainException(
                "Reservation is required.");
        }

        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(
                "Reservation action actor is required.");
        }

        if (occurredAtUtc == default)
        {
            throw new DomainException(
                "Reservation action timestamp is required.");
        }

        if (!IsAllowedTransition(fromStatus, toStatus))
        {
            throw new DomainException(
                "Invalid reservation status transition.");
        }

        var normalizedReason = OrderRules.OptionalText(
            reason,
            ReservationRules.MaximumReasonLength,
            "Reservation action reason");

        if (toStatus == ReservationStatus.Cancelled &&
            normalizedReason is null)
        {
            throw new DomainException(
                "Cancellation reason is required.");
        }

        if (toStatus != ReservationStatus.Cancelled &&
            normalizedReason is not null)
        {
            throw new DomainException(
                "A reason is only supported for cancellation.");
        }

        ReservationId = reservationId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ActorUserId = actorUserId;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
        Reason = normalizedReason;
    }

    public Guid ReservationId { get; private set; }

    public Reservation Reservation { get; private set; } = null!;

    public ReservationStatus? FromStatus { get; private set; }

    public ReservationStatus ToStatus { get; private set; }

    public Guid ActorUserId { get; private set; }

    public User ActorUser { get; private set; } = null!;

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public string? Reason { get; private set; }

    private static bool IsAllowedTransition(
        ReservationStatus? fromStatus,
        ReservationStatus toStatus)
    {
        return (fromStatus, toStatus) switch
        {
            (null, ReservationStatus.Confirmed) => true,

            (ReservationStatus.Confirmed,
                ReservationStatus.Seated) => true,

            (ReservationStatus.Seated,
                ReservationStatus.Completed) => true,

            (ReservationStatus.Confirmed,
                ReservationStatus.Cancelled) => true,

            (ReservationStatus.Confirmed,
                ReservationStatus.NoShow) => true,

            _ => false
        };
    }
}
