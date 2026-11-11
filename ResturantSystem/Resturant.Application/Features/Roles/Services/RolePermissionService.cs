using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Permissions;

namespace Restaurant.Application.Features.Roles.Services;

public sealed class RolePermissionService(
    IRolePermissionDao rolePermissionDao,
    IAuthenticationDao authenticationDao,
    ICurrentUser currentUser,
    IAppDbContext dbContext)
    : IRolePermissionService
{
    public async Task<RolePermissionsResponse>
        SetPermissionsAsync(
            Guid roleId,
            string version,
            Guid[] permissionIds,
            CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var adminRole =
            await rolePermissionDao.LockAdminRoleAsync(
                cancellationToken);

        if (adminRole is null)
        {
            throw new BusinessConflictException(
                "The protected Admin role is unavailable.");
        }

        var isAdmin = await rolePermissionDao.IsActiveAdminAsync(
            currentUser.UserId,
            adminRole.Id,
            cancellationToken);

        var actorAccess =
            await authenticationDao.GetAccessAsync(
                currentUser.UserId,
                cancellationToken);

        if (!isAdmin ||
            actorAccess is null ||
            !actorAccess.Permissions.Contains(
                "Roles.ManagePermissions",
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        var role =
            await rolePermissionDao.GetTrackedRoleAsync(
                roleId,
                cancellationToken);

        if (role is null)
        {
            throw new ResourceNotFoundException("Role");
        }

        var currentVersion =
            await rolePermissionDao.GetRoleVersionAsync(
                roleId,
                cancellationToken);

        var expectedVersion =
            Convert.FromBase64String(version);

        if (!currentVersion.SequenceEqual(expectedVersion))
        {
            throw new BusinessConflictException(
                "The role changed. Reload it before updating.");
        }

        var permissions =
            await rolePermissionDao.GetPermissionsAsync(
                permissionIds,
                cancellationToken);

        if (permissions.Count != permissionIds.Length)
        {
            throw new BusinessConflictException(
                "One or more permission identifiers do not exist.");
        }

        if (role.Id == adminRole.Id)
        {
            var requestedKeys = permissions
                .Select(permission => permission.Key)
                .ToHashSet(StringComparer.Ordinal);

            var removesProtectedPermission =
                PermissionCatalog.All.Any(
                    key => !requestedKeys.Contains(key));

            if (removesProtectedPermission)
            {
                throw new BusinessConflictException(
                    "Core permissions cannot be removed " +
                    "from the protected Admin role.");
            }
        }

        var desiredIds = permissionIds.ToHashSet();

        var existingIds = role.RolePermissions
            .Select(link => link.PermissionId)
            .ToHashSet();

        foreach (var permissionId in existingIds)
        {
            if (!desiredIds.Contains(permissionId))
            {
                role.RevokePermission(permissionId);
            }
        }

        foreach (var permissionId in desiredIds)
        {
            role.GrantPermission(permissionId);
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response =
            await rolePermissionDao.GetRoleDetailsAsync(
                roleId,
                cancellationToken);

        if (response is null)
        {
            throw new ResourceNotFoundException("Role");
        }

        await transaction.CommitAsync(
            cancellationToken);

        return response;
    }
}
