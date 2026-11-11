using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;
using Resturant.Domain.Enums;

namespace Resturant.Infrastructure.DiningTables;

public sealed class DiningTableDao(
    IAppDbContext dbContext)
    : IDiningTableDao
{
    public async Task<PagedResponse<DiningTableResponse>> ListAsync(
        DiningTableListFilter filter,
        CancellationToken cancellationToken)
    {
        var tables = dbContext.DiningTables.AsNoTracking();

        if (filter.Number.HasValue)
        {
            var number = filter.Number.Value;

            tables = tables.Where(table =>
                table.Number == number);
        }

        if (filter.MinimumCapacity.HasValue)
        {
            var minimumCapacity = filter.MinimumCapacity.Value;

            tables = tables.Where(table =>
                table.Capacity >= minimumCapacity);
        }

        if (filter.Status.HasValue)
        {
            var status = filter.Status.Value;

            tables = tables.Where(table =>
                table.Status == status);
        }

        var query = tables.Select(table => new
        {
            table.Id,
            table.Number,
            table.Capacity,
            table.Status,

            HasActiveOrder = dbContext.Orders.Any(order =>
                order.DiningTableId == table.Id &&
                order.Status != OrderStatus.Completed &&
                order.Status != OrderStatus.Cancelled),

            Version = EF.Property<byte[]>(
                table,
                "RowVersion")
        });

        if (filter.CanAcceptOrder.HasValue)
        {
            if (filter.CanAcceptOrder.Value)
            {
                query = query.Where(table =>
                    table.Status == TableStatus.Available &&
                    !table.HasActiveOrder);
            }
            else
            {
                query = query.Where(table =>
                    table.Status != TableStatus.Available ||
                    table.HasActiveOrder);
            }
        }

        var count = await query.CountAsync(cancellationToken);

        var ordered = filter.SortBy.ToLowerInvariant() switch
        {
            "number" => filter.Descending
                ? query.OrderByDescending(table => table.Number)
                : query.OrderBy(table => table.Number),

            "capacity" => filter.Descending
                ? query.OrderByDescending(table => table.Capacity)
                : query.OrderBy(table => table.Capacity),

            _ => throw new ArgumentOutOfRangeException(
                nameof(filter.SortBy),
                "Unsupported dining table sorting field.")
        };

        var rows = await ordered
            .ThenBy(table => table.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(table => new DiningTableResponse(
            table.Id,
            table.Number,
            table.Capacity,
            table.Status.ToString(),
            table.HasActiveOrder,
            table.Status == TableStatus.Available &&
                !table.HasActiveOrder,
            Convert.ToBase64String(table.Version)))
            .ToArray();

        return new PagedResponse<DiningTableResponse>(
            filter.Page,
            filter.PageSize,
            count,
            items);
    }

    public async Task<DiningTableResponse?> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var table = await dbContext.DiningTables
            .AsNoTracking()
            .Where(table => table.Id == id)
            .Select(table => new
            {
                table.Id,
                table.Number,
                table.Capacity,
                table.Status,

                HasActiveOrder = dbContext.Orders.Any(order =>
                    order.DiningTableId == table.Id &&
                    order.Status != OrderStatus.Completed &&
                    order.Status != OrderStatus.Cancelled),

                Version = EF.Property<byte[]>(
                    table,
                    "RowVersion")
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (table is null)
        {
            return null;
        }

        return new DiningTableResponse(
            table.Id,
            table.Number,
            table.Capacity,
            table.Status.ToString(),
            table.HasActiveOrder,
            table.Status == TableStatus.Available &&
                !table.HasActiveOrder,
            Convert.ToBase64String(table.Version));
    }

    public Task<DiningTable?> GetTrackedForUpdateAsync(
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

    public Task<bool> NumberExistsAsync(
        int number,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        var query = dbContext.DiningTables
            .AsNoTracking()
            .Where(table => table.Number == number);

        if (excludingId.HasValue)
        {
            var excludedId = excludingId.Value;

            query = query.Where(table =>
                table.Id != excludedId);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasActiveOrderAsync(
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

    public Task<bool> HasConfirmedReservationAsync(
        Guid tableId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var now = nowUtc.ToUniversalTime();

        return dbContext.Reservations
            .AsNoTracking()
            .AnyAsync(reservation =>
                    reservation.DiningTableId == tableId &&
                    reservation.Status == ReservationStatus.Confirmed &&
                    reservation.EndsAtUtc > now,
                cancellationToken);
    }

    public Task<bool> HasCapacityConflictAsync(
        Guid tableId,
        int proposedCapacity,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var now = nowUtc.ToUniversalTime();

        return dbContext.Reservations
            .AsNoTracking()
            .AnyAsync(reservation =>
                    reservation.DiningTableId == tableId &&
                    reservation.Status == ReservationStatus.Confirmed &&
                    reservation.EndsAtUtc > now &&
                    reservation.GuestCount > proposedCapacity,
                cancellationToken);
    }

    public byte[] GetVersion(DiningTable table)
    {
        var version = dbContext.DiningTables
            .Entry(table)
            .Property<byte[]>("RowVersion")
            .CurrentValue;

        if (version is null || version.Length != 8)
        {
            throw new InvalidOperationException(
                "A persisted dining table rowversion is required.");
        }

        return version;
    }

    public void SetExpectedVersion(
        DiningTable table,
        byte[] version)
    {
        dbContext.DiningTables
            .Entry(table)
            .Property<byte[]>("RowVersion")
            .OriginalValue = version;
    }

    public void Add(DiningTable table)
    {
        dbContext.DiningTables.Add(table);
    }
}