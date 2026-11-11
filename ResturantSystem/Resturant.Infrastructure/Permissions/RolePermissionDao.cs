using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Permissions;

public sealed class RolePermissionDao(
    IAppDbContext dbContext)
    : IRolePermissionDao
{
    public async Task<PagedResponse<RoleSummaryResponse>>
        ListRolesAsync(
            int page,
            int pageSize,
            string? search,
            CancellationToken cancellationToken)
    {
        var query = dbContext.Roles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();

            query = query.Where(role =>
                role.NormalizedName.Contains(term));
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var items = await query
            .OrderBy(role => role.NormalizedName)
            .ThenBy(role => role.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(role => new RoleSummaryResponse(
                role.Id,
                role.Name))
            .ToListAsync(cancellationToken);

        return new PagedResponse<RoleSummaryResponse>(
            page,
            pageSize,
            totalCount,
            items);
    }

    public async Task<RolePermissionsResponse?>
        GetRoleDetailsAsync(
            Guid roleId,
            CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .AsNoTracking()
            .Where(role => role.Id == roleId)
            .Select(role => new
            {
                role.Id,
                role.Name,
                Version = EF.Property<byte[]>(
                    role,
                    "RowVersion")
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (role is null)
        {
            return null;
        }

        var permissions = await dbContext.RolePermissions
            .AsNoTracking()
            .Where(link => link.RoleId == roleId)
            .OrderBy(link => link.Permission.Key)
            .Select(link => new PermissionResponse(
                link.Permission.Id,
                link.Permission.Key,
                link.Permission.DisplayName))
            .ToListAsync(cancellationToken);

        return new RolePermissionsResponse(
            role.Id,
            role.Name,
            Convert.ToBase64String(role.Version),
            permissions);
    }

    public Task<Role?> LockAdminRoleAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [dbo].[Roles] WITH (UPDLOCK, HOLDLOCK)
                WHERE [NormalizedName] = {"ADMIN"}
                """)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> IsActiveAdminAsync(
        Guid userId,
        Guid adminRoleId,
        CancellationToken cancellationToken)
    {
        return dbContext.UserRoles
            .AsNoTracking()
            .AnyAsync(
                link =>
                    link.UserId == userId &&
                    link.RoleId == adminRoleId &&
                    link.User.IsActive,
                cancellationToken);
    }

    public Task<Role?> GetTrackedRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .Include(role => role.RolePermissions)
            .SingleOrDefaultAsync(
                role => role.Id == roleId,
                cancellationToken);
    }

    public Task<byte[]> GetRoleVersionAsync(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .AsNoTracking()
            .Where(role => role.Id == roleId)
            .Select(role => EF.Property<byte[]>(
                role,
                "RowVersion"))
            .SingleAsync(cancellationToken);
    }

    public Task<List<Permission>> GetPermissionsAsync(
        Guid[] permissionIds,
        CancellationToken cancellationToken)
    {
        return dbContext.Permissions
            .AsNoTracking()
            .Where(permission =>
                permissionIds.Contains(permission.Id))
            .ToListAsync(cancellationToken);
    }
}
