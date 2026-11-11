namespace Restaurant.Application.Common.Models;

public sealed record CreateReservationRequest(
    Guid DiningTableId,
    string CustomerName,
    string CustomerPhone,
    int GuestCount,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc)
{
    public override string ToString() =>
        nameof(CreateReservationRequest);
}

public sealed record ReservationResponse(
    Guid Id,
    Guid DiningTableId,
    int TableNumber,
    Guid? CustomerUserId,
    Guid? CreatedByUserId,
    string CustomerName,
    string CustomerPhone,
    int GuestCount,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string Status,
    DateTimeOffset CreatedAtUtc,
    string Version);

public sealed record AvailableReservationTableResponse(
    Guid Id,
    int Number,
    int Capacity);

public sealed record ReservationCreationResult(
    ReservationResponse Reservation,
    bool Replayed);

public sealed record ReservationReceiptLookup(
    Guid ReservationId,
    string RequestHash);