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
using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.Users.Services;

public sealed class UserAccountService(
    IUserAccountDao accountDao,
    IUserAccessDao userAccessDao,
    IRolePermissionDao rolePermissionDao,
    IAuthenticationDao authenticationDao,
    IAuthenticationSessionDao sessionDao,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    IAppDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<UserAccountService> logger)
    : IUserAccountService
{
    public async Task<UserAccessResponse> CreateAsync(
        string fullName,
        string email,
        string password,
        Guid[] roleIds,
        CancellationToken cancellationToken)
    {
        // Perform expensive hashing before acquiring database locks.
        var passwordHash = passwordHasher.Hash(password);

        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        await AuthorizeWriteAsync(
            "Users.Create",
            cancellationToken);

        var normalizedEmail =
            email.Trim().ToUpperInvariant();

        var emailExists = await accountDao.EmailExistsAsync(
            normalizedEmail,
            excludingUserId: null,
            cancellationToken: cancellationToken);

        if (emailExists)
        {
            throw new BusinessConflictException(
                "An account with this email already exists.");
        }

        var roles = await userAccessDao.GetRolesAsync(
            roleIds,
            cancellationToken);

        if (roles.Count != roleIds.Length)
        {
            throw new BusinessConflictException(
                "One or more role identifiers do not exist.");
        }

        var user = new User(
            fullName,
            email,
            passwordHash);

        foreach (var roleId in roleIds)
        {
            user.AssignRole(roleId);
        }

        accountDao.Add(user);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredDetailsAsync(
            user.Id,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "User {CreatedUserId} created by user {ActorUserId}",
            user.Id,
            currentUser.UserId);

        return response;
    }

    public async Task<UserAccessResponse> UpdateProfileAsync(
        Guid userId,
        string fullName,
        string email,
        string version,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        await AuthorizeWriteAsync(
            "Users.Edit",
            cancellationToken);

        var user = await sessionDao.LockUserAsync(
            userId,
            cancellationToken);

        if (user is null)
        {
            throw new ResourceNotFoundException("User");
        }

        var currentVersion =
            await userAccessDao.GetVersionAsync(
                userId,
                cancellationToken);

        var expectedVersion =
            Convert.FromBase64String(version);

        if (!currentVersion.SequenceEqual(expectedVersion))
        {
            throw new BusinessConflictException(
                "The user changed. Reload the user before updating.");
        }

        var normalizedEmail =
            email.Trim().ToUpperInvariant();

        var emailExists = await accountDao.EmailExistsAsync(
            normalizedEmail,
            excludingUserId: userId,
            cancellationToken: cancellationToken);

        if (emailExists)
        {
            throw new BusinessConflictException(
                "An account with this email already exists.");
        }

        var emailChanged =
            user.NormalizedEmail != normalizedEmail;

        user.UpdateProfile(
            fullName,
            email);

        if (emailChanged)
        {
            var sessions =
                await sessionDao.GetUnrevokedSessionsAsync(
                    userId,
                    cancellationToken);

            var now = timeProvider.GetUtcNow();

            foreach (var session in sessions)
            {
                session.Revoke(
                    SessionRevocationReason.EmailChanged,
                    now);
            }
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredDetailsAsync(
            userId,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Profile updated for user {TargetUserId} " +
            "by user {ActorUserId}. EmailChanged: {EmailChanged}",
            userId,
            currentUser.UserId,
            emailChanged);

        return response;
    }

    private async Task AuthorizeWriteAsync(
        string requiredPermission,
        CancellationToken cancellationToken)
    {
        var adminRole =
            await rolePermissionDao.LockAdminRoleAsync(
                cancellationToken);

        if (adminRole is null)
        {
            throw new BusinessConflictException(
                "The protected Admin role is unavailable.");
        }

        var isAdmin =
            await rolePermissionDao.IsActiveAdminAsync(
                currentUser.UserId,
                adminRole.Id,
                cancellationToken);

        var access =
            await authenticationDao.GetAccessAsync(
                currentUser.UserId,
                cancellationToken);

        if (!isAdmin ||
            access is null ||
            !access.Permissions.Contains(
                requiredPermission,
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }
    }

    private async Task<UserAccessResponse>
        GetRequiredDetailsAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        var response =
            await userAccessDao.GetDetailsAsync(
                userId,
                cancellationToken);

        return response
            ?? throw new ResourceNotFoundException("User");
    }
}
