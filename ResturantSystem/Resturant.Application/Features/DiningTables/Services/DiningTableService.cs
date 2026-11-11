using Microsoft.Extensions.Logging;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Common.Security;
using Restaurant.Application.Features.DiningTables;
using Resturant.Domain.Entities;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.DiningTables.Services;

public sealed class DiningTableService(
    IDiningTableDao diningTableDao,
    IAuthenticationDao authenticationDao,
    IAuthenticationSessionDao sessionDao,
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<DiningTableService> logger)
    : IDiningTableService
{
    public async Task<DiningTableResponse> CreateAsync(
        int number,
        int capacity,
        CancellationToken cancellationToken)
    {
        EnsureValidDetails(number, capacity);

        await using var transaction =
            await dbContext.BeginTransactionAsync(cancellationToken);

        var actorId = await GetAuthorizedActorAsync(
            DiningTablePermissions.Create,
            cancellationToken);

        if (await diningTableDao.NumberExistsAsync(
                number,
                excludingId: null,
                cancellationToken))
        {
            throw new BusinessConflictException(
                "A dining table with this number already exists.");
        }

        var table = new DiningTable(number, capacity);

        diningTableDao.Add(table);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetRequiredResponseAsync(
            table.Id,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Dining table {DiningTableId} with number {TableNumber} created by user {UserId}",
            table.Id,
            table.Number,
            actorId);

        return response;
    }

    public async Task<DiningTableResponse> UpdateAsync(
        Guid id,
        int number,
        int capacity,
        string version,
        CancellationToken cancellationToken)
    {
        EnsureValidDetails(number, capacity);

        await using var transaction =
            await dbContext.BeginTransactionAsync(cancellationToken);

        var actorId = await GetAuthorizedActorAsync(
            DiningTablePermissions.Edit,
            cancellationToken);

        var table = await GetEditableTableAsync(
            id,
            version,
            cancellationToken);

        if (await diningTableDao.NumberExistsAsync(
                number,
                table.Id,
                cancellationToken))
        {
            throw new BusinessConflictException(
                "A dining table with this number already exists.");
        }

        if (await diningTableDao.HasCapacityConflictAsync(
                table.Id,
                capacity,
                timeProvider.GetUtcNow(),
                cancellationToken))
        {
            throw new BusinessConflictException(
                "The proposed capacity is below the guest count " +
                "of an existing confirmed reservation.");
        }

        table.UpdateDetails(number, capacity);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetRequiredResponseAsync(
            table.Id,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Dining table {DiningTableId} details updated by user {UserId}",
            table.Id,
            actorId);

        return response;
    }

    public async Task<DiningTableResponse> SetServiceStateAsync(
        Guid id,
        bool isInService,
        string version,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(cancellationToken);

        var actorId = await GetAuthorizedActorAsync(
            DiningTablePermissions.ManageService,
            cancellationToken);

        var table = await GetEditableTableAsync(
            id,
            version,
            cancellationToken);

        if (isInService)
        {
            if (table.Status != TableStatus.OutOfService)
            {
                throw new BusinessConflictException(
                    "The dining table is already in service.");
            }

            // Future confirmed reservations do not prevent
            // restoring an otherwise unoccupied table.
            table.ReturnToService();
        }
        else
        {
            if (table.Status != TableStatus.Available)
            {
                throw new BusinessConflictException(
                    "Only an available table can be taken out of service.");
            }

            if (await diningTableDao.HasConfirmedReservationAsync(
                    table.Id,
                    timeProvider.GetUtcNow(),
                    cancellationToken))
            {
                throw new BusinessConflictException(
                    "The dining table has a confirmed reservation " +
                    "whose time range has not ended.");
            }

            table.TakeOutOfService();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetRequiredResponseAsync(
            table.Id,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Dining table {DiningTableId} service state set to {IsInService} by user {UserId}",
            table.Id,
            isInService,
            actorId);

        return response;
    }

    private async Task<DiningTable> GetEditableTableAsync(
        Guid id,
        string version,
        CancellationToken cancellationToken)
    {
        if (!RowVersionFormat.IsValid(version))
        {
            throw new BusinessConflictException(
                "The dining table version is invalid. Reload the table.");
        }

        var expectedVersion = Convert.FromBase64String(version);

        var table = await diningTableDao.GetTrackedForUpdateAsync(
            id,
            cancellationToken)
            ?? throw new ResourceNotFoundException("Dining table");

        if (!diningTableDao.GetVersion(table)
                .AsSpan()
                .SequenceEqual(expectedVersion))
        {
            throw new BusinessConflictException(
                "The dining table has changed. Reload it and try again.");
        }

        if (table.Status == TableStatus.Occupied)
        {
            throw new BusinessConflictException(
                "An occupied dining table cannot be changed.");
        }

        if (await diningTableDao.HasActiveOrderAsync(
                table.Id,
                cancellationToken))
        {
            throw new BusinessConflictException(
                "The dining table has an active order.");
        }

        if (await diningTableDao.HasSeatedReservationAsync(
                table.Id,
                cancellationToken))
        {
            throw new BusinessConflictException(
                "The dining table has a seated reservation.");
        }

        diningTableDao.SetExpectedVersion(table, expectedVersion);

        return table;
    }

    private async Task<Guid> GetAuthorizedActorAsync(
        string requiredPermission,
        CancellationToken cancellationToken)
    {
        var actorId = currentUser.UserId;

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

        if (!access.Permissions.Contains(
                requiredPermission,
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        return actorId;
    }

    private async Task<DiningTableResponse> GetRequiredResponseAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await diningTableDao.GetAsync(id, cancellationToken)
               ?? throw new ResourceNotFoundException("Dining table");
    }

    private static void EnsureValidDetails(
        int number,
        int capacity)
    {
        if (number <= 0 || capacity < 1 || capacity > 100)
        {
            throw new BusinessConflictException(
                "Table number must be positive and capacity " +
                "must be between 1 and 100.");
        }
    }
}