using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Dao;

public interface IDiningTableDao
{
    Task<PagedResponse<DiningTableResponse>> ListAsync(
        DiningTableListFilter filter,
        CancellationToken cancellationToken);

    Task<DiningTableResponse?> GetAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<DiningTable?> GetTrackedForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<bool> NumberExistsAsync(
        int number,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task<bool> HasActiveOrderAsync(
        Guid tableId,
        CancellationToken cancellationToken);

    Task<bool> HasSeatedReservationAsync(
        Guid tableId,
        CancellationToken cancellationToken);

    Task<bool> HasConfirmedReservationAsync(
        Guid tableId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    Task<bool> HasCapacityConflictAsync(
        Guid tableId,
        int proposedCapacity,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    byte[] GetVersion(DiningTable table);

    void SetExpectedVersion(
        DiningTable table,
        byte[] version);

    void Add(DiningTable table);
}