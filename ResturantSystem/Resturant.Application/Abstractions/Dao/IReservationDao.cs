using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Dao;

public interface IReservationDao
{
    Task<PagedResponse<ReservationResponse>> ListAsync(
        ReservationListFilter filter,
        Guid? customerScopeUserId,
        CancellationToken cancellationToken);

    Task<PagedResponse<AvailableReservationTableResponse>>
        GetAvailabilityAsync(
            DateTimeOffset startsAtUtc,
            DateTimeOffset endsAtUtc,
            int guestCount,
            int page,
            int pageSize,
            CancellationToken cancellationToken);

    Task<ReservationReceiptLookup?> FindReceiptAsync(
        Guid creatorUserId,
        Guid requestKey,
        CancellationToken cancellationToken);

    Task<DiningTable?> LockTableAsync(
        Guid tableId,
        CancellationToken cancellationToken);

    Task<bool> HasOverlapAsync(
        Guid tableId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        CancellationToken cancellationToken);

    Task<ReservationResponse?> GetAsync(
        Guid id,
        Guid? customerScopeUserId,
        CancellationToken cancellationToken);

    Task<Reservation?> GetTrackedForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> HasActiveOrdersAsync(
        Guid tableId,
        CancellationToken cancellationToken);

    Task<bool> HasOtherSeatedReservationsAsync(
        Guid tableId,
        Guid excludingReservationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReservationStatusHistoryResponse>?> GetHistoryAsync(
        Guid id,
        Guid? customerScopeUserId,
        CancellationToken cancellationToken);

    byte[] GetVersion(Reservation reservation);

    void SetExpectedVersion(
        Reservation reservation,
        byte[] version);

    void Add(Reservation reservation);
}