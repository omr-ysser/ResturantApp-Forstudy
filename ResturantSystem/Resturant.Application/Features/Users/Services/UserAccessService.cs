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

public sealed class UserAccessService(
    IUserAccessDao userDao,
    IRolePermissionDao rolePermissionDao,
    IAuthenticationDao authenticationDao,
    IAuthenticationSessionDao sessionDao,
    ICurrentUser currentUser,
    IAppDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<UserAccessService> logger)
    : IUserAccessService
{
    public async Task<UserAccessResponse> SetRolesAsync(
        Guid userId,
        string version,
        Guid[] roleIds,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var adminRoleId =
            await AuthorizeWriteAsync(
                cancellationToken);

        var user = await LockTargetUserAsync(
            userId,
            cancellationToken);

        await EnsureVersionAsync(
            userId,
            version,
            cancellationToken);

        var requestedRoles = await userDao.GetRolesAsync(
            roleIds,
            cancellationToken);

        if (requestedRoles.Count != roleIds.Length)
        {
            throw new BusinessConflictException(
                "One or more role identifiers do not exist.");
        }

        var desiredIds = roleIds.ToHashSet();

        var currentlyHasAdmin = user.UserRoles.Any(
            link => link.RoleId == adminRoleId);

        var removesAdmin =
            currentlyHasAdmin &&
            !desiredIds.Contains(adminRoleId);

        await EnsureNotLastActiveAdminAsync(
            user,
            adminRoleId,
            removesAdmin,
            cancellationToken);

        var existingIds = user.UserRoles
            .Select(link => link.RoleId)
            .ToHashSet();

        foreach (var roleId in existingIds)
        {
            if (!desiredIds.Contains(roleId))
            {
                user.RemoveRole(roleId);
            }
        }

        foreach (var roleId in desiredIds)
        {
            user.AssignRole(roleId);
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredDetailsAsync(
            userId,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Roles updated for user {TargetUserId} by user {ActorUserId}",
            userId,
            currentUser.UserId);

        return response;
    }

    public async Task<UserAccessResponse> SetStatusAsync(
        Guid userId,
        string version,
        bool isActive,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var adminRoleId =
            await AuthorizeWriteAsync(
                cancellationToken);

        var user = await LockTargetUserAsync(
            userId,
            cancellationToken);

        await EnsureVersionAsync(
            userId,
            version,
            cancellationToken);

        var hasAdmin = user.UserRoles.Any(
            link => link.RoleId == adminRoleId);

        var removesActiveAdminAccess =
            hasAdmin && !isActive;

        await EnsureNotLastActiveAdminAsync(
            user,
            adminRoleId,
            removesActiveAdminAccess,
            cancellationToken);

        if (user.IsActive != isActive)
        {
            user.SetActive(isActive);
        }

        if (!isActive)
        {
            var sessions =
                await sessionDao.GetUnrevokedSessionsAsync(
                    userId,
                    cancellationToken);

            var now = timeProvider.GetUtcNow();

            foreach (var session in sessions)
            {
                session.Revoke(
                    SessionRevocationReason.AccountDeactivated,
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
            "User {TargetUserId} active status set to {IsActive} " +
            "by user {ActorUserId}",
            userId,
            isActive,
            currentUser.UserId);

        return response;
    }

    private async Task<Guid> AuthorizeWriteAsync(
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
                "Users.Edit",
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        return adminRole.Id;
    }

    private async Task<User> LockTargetUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var lockedUser = await sessionDao.LockUserAsync(
            userId,
            cancellationToken);

        if (lockedUser is null)
        {
            throw new ResourceNotFoundException("User");
        }

        var user = await userDao.GetTrackedWithRolesAsync(
            userId,
            cancellationToken);

        return user
            ?? throw new ResourceNotFoundException("User");
    }

    private async Task EnsureNotLastActiveAdminAsync(
        User user,
        Guid adminRoleId,
        bool removesAdminAccess,
        CancellationToken cancellationToken)
    {
        if (!user.IsActive || !removesAdminAccess)
        {
            return;
        }

        var activeAdminCount =
            await userDao.CountActiveAdminsAsync(
                adminRoleId,
                cancellationToken);

        if (activeAdminCount <= 1)
        {
            throw new BusinessConflictException(
                "The last active administrator cannot " +
                "lose administrative access.");
        }
    }

    private async Task EnsureVersionAsync(
        Guid userId,
        string expectedVersion,
        CancellationToken cancellationToken)
    {
        var currentVersion =
            await userDao.GetVersionAsync(
                userId,
                cancellationToken);

        var suppliedVersion =
            Convert.FromBase64String(expectedVersion);

        if (!currentVersion.SequenceEqual(suppliedVersion))
        {
            throw new BusinessConflictException(
                "The user changed. Reload the user before updating.");
        }
    }

    private async Task<UserAccessResponse>
        GetRequiredDetailsAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        var response = await userDao.GetDetailsAsync(
            userId,
            cancellationToken);

        return response
            ?? throw new ResourceNotFoundException("User");
    }
}
