using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;
using Resturant.Domain.Enums;

namespace Resturant.Infrastructure.Reservations;

public sealed class ReservationDao(
    IAppDbContext dbContext)
    : IReservationDao
{
    private sealed record ReservationRow(
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
        ReservationStatus Status,
        DateTimeOffset CreatedAtUtc,
        byte[] Version);

    private static readonly Expression<Func<Reservation, ReservationRow>>
        Projection = reservation => new ReservationRow(
            reservation.Id,
            reservation.DiningTableId,
            reservation.DiningTable.Number,
            reservation.CustomerUserId,
            reservation.CreatedByUserId,
            reservation.CustomerName,
            reservation.CustomerPhone,
            reservation.GuestCount,
            reservation.StartsAtUtc,
            reservation.EndsAtUtc,
            reservation.Status,
            reservation.CreatedAtUtc,
            EF.Property<byte[]>(reservation, "RowVersion"));

    public async Task<PagedResponse<ReservationResponse>> ListAsync(
        ReservationListFilter filter,
        Guid? customerScopeUserId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Reservations.AsNoTracking();

        if (customerScopeUserId.HasValue)
        {
            var ownerId = customerScopeUserId.Value;

            query = query.Where(reservation =>
                reservation.CustomerUserId == ownerId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();

            query = query.Where(reservation =>
                reservation.CustomerName.Contains(search) ||
                reservation.CustomerPhone.Contains(search));
        }

        if (filter.Status.HasValue)
        {
            var status = filter.Status.Value;

            query = query.Where(reservation =>
                reservation.Status == status);
        }

        if (filter.DiningTableId.HasValue)
        {
            var tableId = filter.DiningTableId.Value;

            query = query.Where(reservation =>
                reservation.DiningTableId == tableId);
        }

        if (filter.FromUtc.HasValue)
        {
            var from = filter.FromUtc.Value.ToUniversalTime();

            query = query.Where(reservation =>
                reservation.EndsAtUtc > from);
        }

        if (filter.ToUtc.HasValue)
        {
            var to = filter.ToUtc.Value.ToUniversalTime();

            query = query.Where(reservation =>
                reservation.StartsAtUtc < to);
        }

        var count = await query.CountAsync(cancellationToken);

        var ordered = filter.SortBy.ToLowerInvariant() switch
        {
            "startsat" => filter.Descending
                ? query.OrderByDescending(item => item.StartsAtUtc)
                : query.OrderBy(item => item.StartsAtUtc),

            "createdat" => filter.Descending
                ? query.OrderByDescending(item => item.CreatedAtUtc)
                : query.OrderBy(item => item.CreatedAtUtc),

            "tablenumber" => filter.Descending
                ? query.OrderByDescending(item => item.DiningTable.Number)
                : query.OrderBy(item => item.DiningTable.Number),

            _ => throw new ArgumentOutOfRangeException(
                nameof(filter.SortBy),
                "Unsupported reservation sorting field.")
        };

        var rows = await ordered
            .ThenBy(item => item.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ReservationResponse>(
            filter.Page,
            filter.PageSize,
            count,
            rows.Select(Map).ToArray());
    }

    public async Task<PagedResponse<AvailableReservationTableResponse>>
        GetAvailabilityAsync(
            DateTimeOffset startsAtUtc,
            DateTimeOffset endsAtUtc,
            int guestCount,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
    {
        var start = startsAtUtc.ToUniversalTime();
        var end = endsAtUtc.ToUniversalTime();

        var query = dbContext.DiningTables
            .AsNoTracking()
            .Where(table =>
                table.Status != TableStatus.OutOfService &&
                table.Capacity >= guestCount &&
                !dbContext.Reservations.Any(reservation =>
                    reservation.DiningTableId == table.Id &&
                    (
                        reservation.Status == ReservationStatus.Confirmed ||
                        reservation.Status == ReservationStatus.Seated
                    ) &&
                    reservation.StartsAtUtc < end &&
                    reservation.EndsAtUtc > start));

        var count = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(table => table.Capacity)
            .ThenBy(table => table.Number)
            .ThenBy(table => table.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(table => new AvailableReservationTableResponse(
                table.Id,
                table.Number,
                table.Capacity))
            .ToListAsync(cancellationToken);

        return new PagedResponse<AvailableReservationTableResponse>(
            page,
            pageSize,
            count,
            items);
    }

    public Task<ReservationReceiptLookup?> FindReceiptAsync(
        Guid creatorUserId,
        Guid requestKey,
        CancellationToken cancellationToken)
    {
        return dbContext.Reservations
            .AsNoTracking()
            .Where(reservation =>
                reservation.CreatedByUserId == creatorUserId &&
                reservation.RequestKey == requestKey)
            .Select(reservation => new ReservationReceiptLookup(
                reservation.Id,
                reservation.RequestHash!))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<DiningTable?> LockTableAsync(
        Guid tableId,
        CancellationToken cancellationToken)
    {
        return dbContext.DiningTables
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [dbo].[DiningTables] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {tableId}
                """)
            .AsTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasOverlapAsync(
        Guid tableId,
        DateTimeOffset startsAtUtc,
        DateTimeOffset endsAtUtc,
        CancellationToken cancellationToken)
    {
        var start = startsAtUtc.ToUniversalTime();
        var end = endsAtUtc.ToUniversalTime();

        return dbContext.Reservations
            .AsNoTracking()
            .AnyAsync(reservation =>
                    reservation.DiningTableId == tableId &&
                    (
                        reservation.Status == ReservationStatus.Confirmed ||
                        reservation.Status == ReservationStatus.Seated
                    ) &&
                    reservation.StartsAtUtc < end &&
                    reservation.EndsAtUtc > start,
                cancellationToken);
    }

    public async Task<ReservationResponse?> GetAsync(
        Guid id,
        Guid? customerScopeUserId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.Id == id);

        if (customerScopeUserId.HasValue)
        {
            var ownerId = customerScopeUserId.Value;

            query = query.Where(reservation =>
                reservation.CustomerUserId == ownerId);
        }

        var row = await query
            .Select(Projection)
            .SingleOrDefaultAsync(cancellationToken);

        return row is null ? null : Map(row);
    }

    public Task<Reservation?> GetTrackedForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return dbContext.Reservations
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [dbo].[Reservations] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {id}
                """)
            .AsTracking()
            .Include(reservation => reservation.StatusHistory)
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasActiveOrdersAsync(
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

    public Task<bool> HasOtherSeatedReservationsAsync(
        Guid tableId,
        Guid excludingReservationId,
        CancellationToken cancellationToken)
    {
        return dbContext.Reservations
            .AsNoTracking()
            .AnyAsync(reservation =>
                    reservation.DiningTableId == tableId &&
                    reservation.Id != excludingReservationId &&
                    reservation.Status == ReservationStatus.Seated,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ReservationStatusHistoryResponse>?>
        GetHistoryAsync(
            Guid id,
            Guid? customerScopeUserId,
            CancellationToken cancellationToken)
    {
        var query = dbContext.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.Id == id);

        if (customerScopeUserId.HasValue)
        {
            var ownerId = customerScopeUserId.Value;

            query = query.Where(reservation =>
                reservation.CustomerUserId == ownerId);
        }

        if (!await query.AnyAsync(cancellationToken))
        {
            return null;
        }

        var rows = await query
            .SelectMany(reservation => reservation.StatusHistory)
            .OrderBy(entry => entry.OccurredAtUtc)
            .ThenBy(entry => entry.CreatedAtUtc)
            .ThenBy(entry => entry.Id)
            .Select(entry => new
            {
                entry.Id,
                entry.FromStatus,
                entry.ToStatus,
                entry.ActorUserId,
                entry.OccurredAtUtc,
                entry.Reason
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(entry => new ReservationStatusHistoryResponse(
                entry.Id,
                entry.FromStatus?.ToString(),
                entry.ToStatus.ToString(),
                entry.ActorUserId,
                entry.OccurredAtUtc,
                entry.Reason))
            .ToArray();
    }

    public byte[] GetVersion(Reservation reservation)
    {
        var version = dbContext.Reservations
            .Entry(reservation)
            .Property<byte[]>("RowVersion")
            .CurrentValue;

        if (version is null || version.Length != 8)
        {
            throw new InvalidOperationException(
                "A persisted reservation rowversion is required.");
        }

        return version;
    }

    public void SetExpectedVersion(
        Reservation reservation,
        byte[] version)
    {
        dbContext.Reservations
            .Entry(reservation)
            .Property<byte[]>("RowVersion")
            .OriginalValue = version;
    }

    public void Add(Reservation reservation)
    {
        if (!reservation.CreatedByUserId.HasValue ||
            !reservation.RequestKey.HasValue ||
            string.IsNullOrWhiteSpace(reservation.RequestHash) ||
            reservation.StatusHistory.Count == 0)
        {
            throw new InvalidOperationException(
                "New reservations require creation metadata " +
                "and initial status history.");
        }

        dbContext.Reservations.Add(reservation);
    }

    private static ReservationResponse Map(ReservationRow row)
    {
        return new ReservationResponse(
            row.Id,
            row.DiningTableId,
            row.TableNumber,
            row.CustomerUserId,
            row.CreatedByUserId,
            row.CustomerName,
            row.CustomerPhone,
            row.GuestCount,
            row.StartsAtUtc,
            row.EndsAtUtc,
            row.Status.ToString(),
            row.CreatedAtUtc,
            Convert.ToBase64String(row.Version));
    }
}