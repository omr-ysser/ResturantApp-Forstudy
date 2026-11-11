using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Permissions;

public sealed class RoleManagementDao(
    IAppDbContext dbContext)
    : IRoleManagementDao
{
    public Task<bool> NameExistsAsync(
        string normalizedName,
        Guid? excludingRoleId,
        CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .AsNoTracking()
            .AnyAsync(
                role =>
                    role.NormalizedName == normalizedName &&
                    (!excludingRoleId.HasValue ||
                     role.Id != excludingRoleId.Value),
                cancellationToken);
    }

    public Task<Role?> GetTrackedAsync(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .SingleOrDefaultAsync(
                role => role.Id == roleId,
                cancellationToken);
    }

    public Task<byte[]> GetVersionAsync(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .AsNoTracking()
            .Where(role => role.Id == roleId)
            .Select(role =>
                EF.Property<byte[]>(
                    role,
                    "RowVersion"))
            .SingleAsync(cancellationToken);
    }

    public Task<bool> HasUsersAsync(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        return dbContext.UserRoles
            .AsNoTracking()
            .AnyAsync(
                link => link.RoleId == roleId,
                cancellationToken);
    }

    public void Add(Role role)
    {
        dbContext.Roles.Add(role);
    }

    public void Remove(Role role)
    {
        dbContext.Roles.Remove(role);
    }
}
