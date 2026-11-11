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
using Restaurant.Application.Common.Security;
using Resturant.Domain.Entities;

namespace Restaurant.Application.Features.Roles.Services;

public sealed class RoleManagementService(
    IRoleManagementDao roleDao,
    IRolePermissionDao rolePermissionDao,
    IAuthenticationDao authenticationDao,
    ICurrentUser currentUser,
    IAppDbContext dbContext)
    : IRoleManagementService
{
    public async Task<RolePermissionsResponse> CreateAsync(
        string name,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        await AuthorizeWriteAsync(
            "Roles.Create",
            cancellationToken);

        var normalizedName =
            name.Trim().ToUpperInvariant();

        if (RolePolicy.IsSystemRole(normalizedName))
        {
            throw new BusinessConflictException(
                "This role name is reserved for a system role.");
        }

        var exists = await roleDao.NameExistsAsync(
            normalizedName,
            excludingRoleId: null,
            cancellationToken: cancellationToken);

        if (exists)
        {
            throw new BusinessConflictException(
                "A role with this name already exists.");
        }

        var role = new Role(name);

        roleDao.Add(role);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredDetailsAsync(
            role.Id,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return response;
    }

    public async Task<RolePermissionsResponse> UpdateAsync(
        Guid roleId,
        string name,
        string version,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        await AuthorizeWriteAsync(
            "Roles.Edit",
            cancellationToken);

        var role = await roleDao.GetTrackedAsync(
            roleId,
            cancellationToken);

        if (role is null)
        {
            throw new ResourceNotFoundException("Role");
        }

        if (RolePolicy.IsSystemRole(role.NormalizedName))
        {
            throw new BusinessConflictException(
                "System roles cannot be renamed.");
        }

        await EnsureVersionAsync(
            roleId,
            version,
            cancellationToken);

        var normalizedName =
            name.Trim().ToUpperInvariant();

        if (RolePolicy.IsSystemRole(normalizedName))
        {
            throw new BusinessConflictException(
                "This role name is reserved for a system role.");
        }

        var exists = await roleDao.NameExistsAsync(
            normalizedName,
            excludingRoleId: roleId,
            cancellationToken: cancellationToken);

        if (exists)
        {
            throw new BusinessConflictException(
                "A role with this name already exists.");
        }

        role.Rename(name);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredDetailsAsync(
            roleId,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return response;
    }

    public async Task DeleteAsync(
        Guid roleId,
        string version,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        await AuthorizeWriteAsync(
            "Roles.Delete",
            cancellationToken);

        var role = await roleDao.GetTrackedAsync(
            roleId,
            cancellationToken);

        if (role is null)
        {
            throw new ResourceNotFoundException("Role");
        }

        if (RolePolicy.IsSystemRole(role.NormalizedName))
        {
            throw new BusinessConflictException(
                "System roles cannot be deleted.");
        }

        await EnsureVersionAsync(
            roleId,
            version,
            cancellationToken);

        var hasUsers = await roleDao.HasUsersAsync(
            roleId,
            cancellationToken);

        if (hasUsers)
        {
            throw new BusinessConflictException(
                "Remove this role from all users " +
                "before deleting it.");
        }

        roleDao.Remove(role);

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

    private async Task EnsureVersionAsync(
        Guid roleId,
        string expectedVersion,
        CancellationToken cancellationToken)
    {
        var currentVersion =
            await roleDao.GetVersionAsync(
                roleId,
                cancellationToken);

        var suppliedVersion =
            Convert.FromBase64String(expectedVersion);

        if (!currentVersion.SequenceEqual(suppliedVersion))
        {
            throw new BusinessConflictException(
                "The role changed. Reload it before updating.");
        }
    }

    private async Task<RolePermissionsResponse>
        GetRequiredDetailsAsync(
            Guid roleId,
            CancellationToken cancellationToken)
    {
        var response =
            await rolePermissionDao.GetRoleDetailsAsync(
                roleId,
                cancellationToken);

        return response
            ?? throw new ResourceNotFoundException("Role");
    }
}
