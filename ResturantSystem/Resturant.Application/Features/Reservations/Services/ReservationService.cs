using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Common.Security;
using Restaurant.Application.Features.Reservations;
using Resturant.Domain.Common;
using Resturant.Domain.Entities;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.Reservations.Services;

public sealed class ReservationService(
    IReservationDao reservationDao,
    IAuthenticationDao authenticationDao,
    IAuthenticationSessionDao sessionDao,
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<ReservationService> logger)
    : IReservationService
{
    public async Task<ReservationCreationResult> CreateAsync(
        Guid requestKey,
        CreateReservationRequest request,
        CancellationToken cancellationToken)
    {
        var requestHash = CreateFingerprint(request);

        await using var transaction =
            await dbContext.BeginTransactionAsync(cancellationToken);

        var access = await GetActiveAccessAsync(cancellationToken);

        if (!HasPermission(access, ReservationPermissions.Create))
        {
            throw new ForbiddenOperationException();
        }

        var userId = access.Id;

        var receipt = await reservationDao.FindReceiptAsync(
            userId,
            requestKey,
            cancellationToken);

        if (receipt is not null)
        {
            if (!string.Equals(
                    receipt.RequestHash,
                    requestHash,
                    StringComparison.Ordinal))
            {
                throw new BusinessConflictException(
                    "The idempotency key was already used " +
                    "with a different reservation request.");
            }

            var existing = await GetRequiredResponseAsync(
                receipt.ReservationId,
                userId,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Reservation {ReservationId} replayed for user {UserId}",
                existing.Id,
                userId);

            return new ReservationCreationResult(existing, true);
        }

        var table = await reservationDao.LockTableAsync(
            request.DiningTableId,
            cancellationToken)
            ?? throw new ResourceNotFoundException("Dining table");

        if (table.Status == TableStatus.OutOfService)
        {
            throw new BusinessConflictException(
                "The dining table is out of service.");
        }

        if (request.GuestCount > table.Capacity)
        {
            throw new BusinessConflictException(
                "The guest count exceeds the dining table capacity.");
        }

        if (await reservationDao.HasOverlapAsync(
                table.Id,
                request.StartsAtUtc,
                request.EndsAtUtc,
                cancellationToken))
        {
            throw new BusinessConflictException(
                "The dining table already has an overlapping reservation.");
        }

        var reservation = new Reservation(
            table.Id,
            request.CustomerName,
            request.CustomerPhone,
            request.GuestCount,
            request.StartsAtUtc,
            request.EndsAtUtc,
            userId,
            userId,
            requestKey,
            requestHash,
            timeProvider.GetUtcNow());

        reservationDao.Add(reservation);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetRequiredResponseAsync(
            reservation.Id,
            userId,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Reservation {ReservationId} created for table {DiningTableId} by user {UserId}",
            reservation.Id,
            table.Id,
            userId);

        return new ReservationCreationResult(response, false);
    }

    public async Task<ReservationResponse> ChangeStateAsync(
        Guid id,
        ReservationStatus targetStatus,
        string version,
        string? reason,
        CancellationToken cancellationToken)
    {
        if (!RowVersionFormat.IsValid(version))
        {
            throw new BusinessConflictException(
                "The reservation version is invalid. Reload the reservation.");
        }

        var expectedVersion = Convert.FromBase64String(version);

        await using var transaction =
            await dbContext.BeginTransactionAsync(cancellationToken);

        var access = await GetActiveAccessAsync(cancellationToken);

        var actorId = access.Id;

        var isCancellation =
            targetStatus == ReservationStatus.Cancelled;

        var canCancelAll = HasPermission(
            access,
            ReservationPermissions.CancelAll);

        if (isCancellation)
        {
            if (!canCancelAll &&
                !HasPermission(access, ReservationPermissions.CancelOwn))
            {
                throw new ForbiddenOperationException();
            }
        }
        else if (!HasPermission(access, ReservationPermissions.Manage))
        {
            throw new ForbiddenOperationException();
        }

        Guid? customerScope =
            isCancellation && !canCancelAll
                ? actorId
                : null;

        var snapshot = await reservationDao.GetAsync(
            id,
            customerScope,
            cancellationToken)
            ?? throw new ResourceNotFoundException("Reservation");

        // Lock order: account -> dining table -> reservation.
        var table = await reservationDao.LockTableAsync(
            snapshot.DiningTableId,
            cancellationToken)
            ?? throw new ResourceNotFoundException("Dining table");

        var reservation =
            await reservationDao.GetTrackedForUpdateAsync(
                id,
                cancellationToken)
            ?? throw new ResourceNotFoundException("Reservation");

        if (customerScope.HasValue &&
            reservation.CustomerUserId != customerScope.Value)
        {
            throw new ResourceNotFoundException("Reservation");
        }

        if (reservation.DiningTableId != table.Id)
        {
            throw new BusinessConflictException(
                "The reservation table changed. Reload the reservation.");
        }

        if (!reservationDao.GetVersion(reservation)
                .AsSpan()
                .SequenceEqual(expectedVersion))
        {
            throw new BusinessConflictException(
                "The reservation has changed. Reload it and try again.");
        }

        reservationDao.SetExpectedVersion(
            reservation,
            expectedVersion);

        var now = timeProvider.GetUtcNow();

        switch (targetStatus)
        {
            case ReservationStatus.Cancelled:
                EnsureStatus(reservation, ReservationStatus.Confirmed);

                if (!canCancelAll && now >= reservation.StartsAtUtc)
                {
                    throw new BusinessConflictException(
                        "Customers can only cancel before the reservation starts.");
                }

                reservation.Cancel(reason!, actorId, now);
                break;

            case ReservationStatus.Seated:
                EnsureStatus(reservation, ReservationStatus.Confirmed);

                var elapsed = now - reservation.StartsAtUtc;

                if (elapsed < -ReservationRules.EarlySeatingWindow ||
                    elapsed >= ReservationRules.NoShowGracePeriod)
                {
                    throw new BusinessConflictException(
                        "The reservation is outside the seating window.");
                }

                if (table.Status != TableStatus.Available)
                {
                    throw new BusinessConflictException(
                        "The dining table is not available for seating.");
                }

                if (reservation.GuestCount > table.Capacity)
                {
                    throw new BusinessConflictException(
                        "The guest count exceeds the current table capacity.");
                }

                await EnsureTableHasNoActiveOrdersAsync(
                    table.Id,
                    cancellationToken);

                await EnsureNoOtherSeatedReservationsAsync(
                    table.Id,
                    reservation.Id,
                    cancellationToken);

                reservation.Seat(actorId, now);
                table.Occupy();
                break;

            case ReservationStatus.Completed:
                EnsureStatus(reservation, ReservationStatus.Seated);

                await EnsureTableHasNoActiveOrdersAsync(
                    table.Id,
                    cancellationToken);

                await EnsureNoOtherSeatedReservationsAsync(
                    table.Id,
                    reservation.Id,
                    cancellationToken);

                reservation.Complete(actorId, now);

                if (table.Status == TableStatus.Occupied)
                {
                    table.Release();
                }

                break;

            case ReservationStatus.NoShow:
                EnsureStatus(reservation, ReservationStatus.Confirmed);

                if (now - reservation.StartsAtUtc <
                    ReservationRules.NoShowGracePeriod)
                {
                    throw new BusinessConflictException(
                        "No-show can only be recorded at least " +
                        "15 minutes after the reservation starts.");
                }

                reservation.MarkNoShow(actorId, now);
                break;

            default:
                throw new BusinessConflictException(
                    "The requested reservation action is not supported.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetRequiredResponseAsync(
            reservation.Id,
            customerScope,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Reservation {ReservationId} changed from {PreviousStatus} to {NewStatus} by user {UserId}",
            reservation.Id,
            snapshot.Status,
            targetStatus,
            actorId);

        return response;
    }

    private async Task<AccessProfile> GetActiveAccessAsync(
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        await sessionDao.LockUserAsync(userId, cancellationToken);

        var access = await authenticationDao.GetAccessAsync(
            userId,
            cancellationToken);

        if (access is null ||
            !await sessionDao.IsSessionActiveAsync(
                userId,
                currentUser.SessionId,
                timeProvider.GetUtcNow(),
                cancellationToken))
        {
            throw new AuthenticationFailedException();
        }

        return access;
    }

    private async Task EnsureTableHasNoActiveOrdersAsync(
        Guid tableId,
        CancellationToken cancellationToken)
    {
        if (await reservationDao.HasActiveOrdersAsync(
                tableId,
                cancellationToken))
        {
            throw new BusinessConflictException(
                "The dining table has an active order.");
        }
    }

    private async Task EnsureNoOtherSeatedReservationsAsync(
        Guid tableId,
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        if (await reservationDao.HasOtherSeatedReservationsAsync(
                tableId,
                reservationId,
                cancellationToken))
        {
            throw new BusinessConflictException(
                "The dining table has another seated reservation.");
        }
    }

    private async Task<ReservationResponse> GetRequiredResponseAsync(
        Guid id,
        Guid? customerScope,
        CancellationToken cancellationToken)
    {
        return await reservationDao.GetAsync(
                   id,
                   customerScope,
                   cancellationToken)
               ?? throw new ResourceNotFoundException("Reservation");
    }

    private static bool HasPermission(
        AccessProfile access,
        string permission)
    {
        return access.Permissions.Contains(
            permission,
            StringComparer.Ordinal);
    }

    private static void EnsureStatus(
        Reservation reservation,
        ReservationStatus expected)
    {
        if (reservation.Status != expected)
        {
            throw new BusinessConflictException(
                $"This action requires a {expected} reservation.");
        }
    }

    private static string CreateFingerprint(
        CreateReservationRequest request)
    {
        var canonicalRequest = new
        {
            SchemaVersion = 1,
            request.DiningTableId,
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            request.GuestCount,
            StartsAtUtc = request.StartsAtUtc.ToUniversalTime(),
            EndsAtUtc = request.EndsAtUtc.ToUniversalTime()
        };

        return Convert.ToHexString(
            SHA256.HashData(
                JsonSerializer.SerializeToUtf8Bytes(canonicalRequest)));
    }
}