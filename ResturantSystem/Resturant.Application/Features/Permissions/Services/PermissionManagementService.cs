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
using Resturant.Domain.Entities;

namespace Restaurant.Application.Features.Permissions.Services;

public sealed class PermissionManagementService(
    IPermissionManagementDao permissionDao,
    IRolePermissionDao rolePermissionDao,
    IAuthenticationDao authenticationDao,
    ICurrentUser currentUser,
    IAppDbContext dbContext)
    : IPermissionManagementService
{
    public async Task<PermissionDetailsResponse> CreateAsync(
        string key,
        string displayName,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        await AuthorizeWriteAsync(
            "Permissions.Create",
            cancellationToken);

        var normalizedKey =
            key.Trim().ToUpperInvariant();

        var exists = await permissionDao.ExistsAsync(
            normalizedKey,
            cancellationToken);

        if (exists)
        {
            throw new BusinessConflictException(
                "A permission with this key already exists.");
        }

        var permission = new Permission(
            key,
            displayName);

        permissionDao.Add(permission);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredDetailsAsync(
            permission.Id,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return response;
    }

    public async Task<PermissionDetailsResponse> UpdateAsync(
        Guid permissionId,
        string displayName,
        string version,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        await AuthorizeWriteAsync(
            "Permissions.Edit",
            cancellationToken);

        var permission =
            await permissionDao.GetTrackedAsync(
                permissionId,
                cancellationToken);

        if (permission is null)
        {
            throw new ResourceNotFoundException(
                "Permission");
        }

        await EnsureVersionAsync(
            permissionId,
            version,
            cancellationToken);

        permission.UpdateDisplayName(displayName);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredDetailsAsync(
            permissionId,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return response;
    }

    public async Task DeleteAsync(
        Guid permissionId,
        string version,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        await AuthorizeWriteAsync(
            "Permissions.Delete",
            cancellationToken);

        var permission =
            await permissionDao.GetTrackedAsync(
                permissionId,
                cancellationToken);

        if (permission is null)
        {
            throw new ResourceNotFoundException(
                "Permission");
        }

        await EnsureVersionAsync(
            permissionId,
            version,
            cancellationToken);

        var isSystemPermission = PermissionCatalog.All.Contains(
            permission.Key,
            StringComparer.OrdinalIgnoreCase);

        if (isSystemPermission)
        {
            throw new BusinessConflictException(
                "System permissions cannot be deleted.");
        }

        var isAssigned = await permissionDao.IsAssignedAsync(
            permissionId,
            cancellationToken);

        if (isAssigned)
        {
            throw new BusinessConflictException(
                "Remove this permission from all roles " +
                "before deleting it.");
        }

        permissionDao.Remove(permission);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
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

        var isAdmin = await rolePermissionDao.IsActiveAdminAsync(
            currentUser.UserId,
            adminRole.Id,
            cancellationToken);

        var access = await authenticationDao.GetAccessAsync(
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

    private async Task EnsureVersionAsync(
        Guid permissionId,
        string expectedVersion,
        CancellationToken cancellationToken)
    {
        var currentVersion =
            await permissionDao.GetVersionAsync(
                permissionId,
                cancellationToken);

        var suppliedVersion =
            Convert.FromBase64String(expectedVersion);

        if (!currentVersion.SequenceEqual(suppliedVersion))
        {
            throw new BusinessConflictException(
                "The permission changed. Reload it before updating.");
        }
    }

    private async Task<PermissionDetailsResponse>
        GetRequiredDetailsAsync(
            Guid permissionId,
            CancellationToken cancellationToken)
    {
        var response =
            await permissionDao.GetDetailsAsync(
                permissionId,
                cancellationToken);

        return response
            ?? throw new ResourceNotFoundException(
                "Permission");
    }
}
