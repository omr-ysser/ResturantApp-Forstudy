using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Exceptions;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.Authentication.Services;

public sealed class PasswordChangeService(
    IPasswordCredentialDao credentialDao,
    IAuthenticationSessionDao sessionDao,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IAppDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<PasswordChangeService> logger)
    : IPasswordChangeService
{
    public async Task ChangeAsync(
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var sessionId = currentUser.SessionId;

        var credential = await credentialDao.FindAsync(
            userId,
            cancellationToken);

        if (credential is null ||
            !credential.IsActive)
        {
            throw new AuthenticationFailedException();
        }

        var validPassword = passwordHasher.Verify(
            currentPassword,
            credential.PasswordHash);

        if (!validPassword)
        {
            throw new AuthenticationFailedException();
        }

        if (string.Equals(
                currentPassword,
                newPassword,
                StringComparison.Ordinal))
        {
            throw new BusinessConflictException(
                "The new password must differ from the current password.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Perform expensive hashing before acquiring the user lock.
        var newPasswordHash = passwordHasher.Hash(
            newPassword);

        cancellationToken.ThrowIfCancellationRequested();

        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var user = await sessionDao.LockUserAsync(
            userId,
            cancellationToken);

        if (user is null ||
            !user.IsActive ||
            user.PasswordHash != credential.PasswordHash)
        {
            throw new AuthenticationFailedException();
        }

        var now = timeProvider.GetUtcNow();

        var sessionIsActive =
            await sessionDao.IsSessionActiveAsync(
                userId,
                sessionId,
                now,
                cancellationToken);

        if (!sessionIsActive)
        {
            throw new AuthenticationFailedException();
        }

        user.ChangePasswordHash(
            newPasswordHash);

        var sessions =
            await sessionDao.GetUnrevokedSessionsAsync(
                userId,
                cancellationToken);

        foreach (var session in sessions)
        {
            session.Revoke(
                SessionRevocationReason.PasswordChanged,
                now);
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Password changed and sessions revoked for user {UserId}",
            userId);
    }
}
