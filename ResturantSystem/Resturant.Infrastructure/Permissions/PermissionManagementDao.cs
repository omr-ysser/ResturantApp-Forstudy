using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Permissions;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Permissions;

public sealed class PermissionManagementDao(
    IAppDbContext dbContext)
    : IPermissionManagementDao
{
    public async Task<PermissionDetailsResponse?> GetDetailsAsync(
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        var permission = await dbContext.Permissions
            .AsNoTracking()
            .Where(permission =>
                permission.Id == permissionId)
            .Select(permission => new
            {
                permission.Id,
                permission.Key,
                permission.DisplayName,
                Version = EF.Property<byte[]>(
                    permission,
                    "RowVersion")
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (permission is null)
        {
            return null;
        }

        var isSystemPermission = PermissionCatalog.All.Contains(
            permission.Key,
            StringComparer.OrdinalIgnoreCase);

        return new PermissionDetailsResponse(
            permission.Id,
            permission.Key,
            permission.DisplayName,
            Convert.ToBase64String(permission.Version),
            isSystemPermission);
    }

    public Task<Permission?> GetTrackedAsync(
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        return dbContext.Permissions
            .SingleOrDefaultAsync(
                permission =>
                    permission.Id == permissionId,
                cancellationToken);
    }

    public Task<byte[]> GetVersionAsync(
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        return dbContext.Permissions
            .AsNoTracking()
            .Where(permission =>
                permission.Id == permissionId)
            .Select(permission =>
                EF.Property<byte[]>(
                    permission,
                    "RowVersion"))
            .SingleAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(
        string normalizedKey,
        CancellationToken cancellationToken)
    {
        return dbContext.Permissions
            .AsNoTracking()
            .AnyAsync(
                permission =>
                    permission.NormalizedKey == normalizedKey,
                cancellationToken);
    }

    public Task<bool> IsAssignedAsync(
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        return dbContext.RolePermissions
            .AsNoTracking()
            .AnyAsync(
                link =>
                    link.PermissionId == permissionId,
                cancellationToken);
    }

    public void Add(Permission permission)
    {
        dbContext.Permissions.Add(permission);
    }

    public void Remove(Permission permission)
    {
        dbContext.Permissions.Remove(permission);
    }
}
