using Resturant.Domain.Common;
using Resturant.Domain.Enums;
using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class Reservation : BaseEntity
{
    private readonly List<ReservationStatusHistory> _statusHistory = [];

    private Reservation()
    {
    }

    public Reservation(
        Guid diningTableId,
        string customerName,
        string customerPhone,
        int guestCount,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        Guid createdByUserId,
        Guid? customerUserId,
        Guid requestKey,
        string requestHash,
        DateTimeOffset nowUtc)
    {
        if (diningTableId == Guid.Empty)
        {
            throw new DomainException(
                "Dining table is required.");
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new DomainException(
                "Reservation creator is required.");
        }

        if (customerUserId.HasValue &&
            customerUserId.Value == Guid.Empty)
        {
            throw new DomainException(
                "Customer account identifier is invalid.");
        }

        if (requestKey == Guid.Empty)
        {
            throw new DomainException(
                "Reservation idempotency key is required.");
        }

        if (!IsValidRequestHash(requestHash))
        {
            throw new DomainException(
                "Request hash must be a 64-character hexadecimal SHA-256 hash.");
        }

        if (guestCount < 1 ||
            guestCount > ReservationRules.MaximumGuestCount)
        {
            throw new DomainException(
                $"Guest count must be between 1 and " +
                $"{ReservationRules.MaximumGuestCount}.");
        }

        if (nowUtc == default)
        {
            throw new DomainException(
                "Current timestamp is required.");
        }

        var now = nowUtc.ToUniversalTime();
        var start = startsAt.ToUniversalTime();
        var end = endsAt.ToUniversalTime();

        var leadTime = start - now;

        if (leadTime < ReservationRules.MinimumLeadTime)
        {
            throw new DomainException(
                "Reservations require at least 15 minutes of lead time.");
        }

        if (leadTime > ReservationRules.MaximumAdvance)
        {
            throw new DomainException(
                "Reservations cannot start more than 30 days in advance.");
        }

        var duration = end - start;

        if (duration < ReservationRules.MinimumDuration ||
            duration > ReservationRules.MaximumDuration)
        {
            throw new DomainException(
                "Reservation duration must be between 30 minutes and 4 hours.");
        }

        DiningTableId = diningTableId;

        CustomerName = OrderRules.RequiredText(
            customerName,
            ReservationRules.MaximumCustomerNameLength,
            "Customer name");

        CustomerPhone = OrderRules.RequiredText(
            customerPhone,
            ReservationRules.MaximumCustomerPhoneLength,
            "Customer phone");

        GuestCount = guestCount;
        StartsAtUtc = start;
        EndsAtUtc = end;

        CreatedByUserId = createdByUserId;
        CustomerUserId = customerUserId;
        RequestKey = requestKey;
        RequestHash = requestHash.ToUpperInvariant();

        _statusHistory.Add(
            new ReservationStatusHistory(
                Id,
                fromStatus: null,
                toStatus: ReservationStatus.Confirmed,
                actorUserId: createdByUserId,
                occurredAtUtc: now));
    }

    public Guid DiningTableId { get; private set; }

    public DiningTable DiningTable { get; private set; } = null!;

    public string CustomerName { get; private set; } = string.Empty;

    public string CustomerPhone { get; private set; } = string.Empty;

    public int GuestCount { get; private set; }

    public DateTimeOffset StartsAtUtc { get; private set; }

    public DateTimeOffset EndsAtUtc { get; private set; }

    public ReservationStatus Status { get; private set; }
        = ReservationStatus.Confirmed;

    // Nullable to preserve legacy reservations without
    // inventing ownership or creation information.
    public Guid? CreatedByUserId { get; private set; }

    public User? CreatedByUser { get; private set; }

    public Guid? CustomerUserId { get; private set; }

    public User? CustomerUser { get; private set; }

    public Guid? RequestKey { get; private set; }

    public string? RequestHash { get; private set; }

    public IReadOnlyCollection<ReservationStatusHistory> StatusHistory =>
        _statusHistory.AsReadOnly();

    public void Seat(
        Guid actorUserId,
        DateTimeOffset occurredAtUtc)
    {
        EnsureConfirmed();

        var elapsed = occurredAtUtc.ToUniversalTime()
            - StartsAtUtc;

        if (elapsed < -ReservationRules.EarlySeatingWindow ||
            elapsed >= ReservationRules.NoShowGracePeriod)
        {
            throw new DomainException(
                "Seating is allowed from 15 minutes before " +
                "the reservation until less than 15 minutes after its start.");
        }

        Transition(
            ReservationStatus.Confirmed,
            ReservationStatus.Seated,
            actorUserId,
            occurredAtUtc);
    }

    public void Complete(
        Guid actorUserId,
        DateTimeOffset occurredAtUtc)
    {
        Transition(
            ReservationStatus.Seated,
            ReservationStatus.Completed,
            actorUserId,
            occurredAtUtc);
    }

    public void Cancel(
        string reason,
        Guid actorUserId,
        DateTimeOffset occurredAtUtc)
    {
        EnsureConfirmed();

        var normalizedReason = OrderRules.RequiredText(
            reason,
            ReservationRules.MaximumReasonLength,
            "Cancellation reason");

        Transition(
            ReservationStatus.Confirmed,
            ReservationStatus.Cancelled,
            actorUserId,
            occurredAtUtc,
            normalizedReason);
    }

    public void MarkNoShow(
        Guid actorUserId,
        DateTimeOffset occurredAtUtc)
    {
        EnsureConfirmed();

        var elapsed = occurredAtUtc.ToUniversalTime()
            - StartsAtUtc;

        if (elapsed < ReservationRules.NoShowGracePeriod)
        {
            throw new DomainException(
                "No-show can only be recorded at least " +
                "15 minutes after the reservation starts.");
        }

        Transition(
            ReservationStatus.Confirmed,
            ReservationStatus.NoShow,
            actorUserId,
            occurredAtUtc);
    }

    private void EnsureConfirmed()
    {
        if (Status != ReservationStatus.Confirmed)
        {
            throw new DomainException(
                "This action requires a confirmed reservation.");
        }
    }

    private void Transition(
        ReservationStatus expectedStatus,
        ReservationStatus nextStatus,
        Guid actorUserId,
        DateTimeOffset occurredAtUtc,
        string? reason = null)
    {
        if (Status != expectedStatus)
        {
            throw new DomainException(
                $"Cannot change reservation from {Status} to {nextStatus}.");
        }

        if (occurredAtUtc == default)
        {
            throw new DomainException(
                "Reservation action timestamp is required.");
        }

        var utc = occurredAtUtc.ToUniversalTime();

        var latestKnownTime = _statusHistory.Count == 0
            ? CreatedAtUtc
            : _statusHistory.Max(entry => entry.OccurredAtUtc);

        if (utc < latestKnownTime)
        {
            throw new DomainException(
                "Reservation action timestamp cannot precede its history.");
        }

        var history = new ReservationStatusHistory(
            Id,
            Status,
            nextStatus,
            actorUserId,
            utc,
            reason);

        Status = nextStatus;

        _statusHistory.Add(history);

        MarkUpdated();
    }

    private static bool IsValidRequestHash(string? value)
    {
        if (value is null || value.Length != 64)
        {
            return false;
        }

        foreach (var character in value)
        {
            var isHex =
                character is >= '0' and <= '9'
                or >= 'A' and <= 'F'
                or >= 'a' and <= 'f';

            if (!isHex)
            {
                return false;
            }
        }

        return true;
    }
}