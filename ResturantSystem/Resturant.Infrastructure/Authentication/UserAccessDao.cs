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

namespace Resturant.Infrastructure.Authentication;

public sealed class UserAccessDao(
    IAppDbContext dbContext)
    : IUserAccessDao
{
    public async Task<PagedResponse<UserSummaryResponse>> ListAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();

            query = query.Where(user =>
                user.NormalizedEmail.Contains(term) ||
                user.FullName.ToUpper().Contains(term));
        }

        if (isActive.HasValue)
        {
            query = query.Where(user =>
                user.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var items = await query
            .OrderBy(user => user.NormalizedEmail)
            .ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new UserSummaryResponse(
                user.Id,
                user.FullName,
                user.Email,
                user.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResponse<UserSummaryResponse>(
            page,
            pageSize,
            totalCount,
            items);
    }

    public async Task<UserAccessResponse?> GetDetailsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.IsActive,
                Version = EF.Property<byte[]>(
                    user,
                    "RowVersion")
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await dbContext.UserRoles
            .AsNoTracking()
            .Where(link => link.UserId == userId)
            .OrderBy(link => link.Role.NormalizedName)
            .ThenBy(link => link.RoleId)
            .Select(link => new RoleSummaryResponse(
                link.Role.Id,
                link.Role.Name))
            .ToListAsync(cancellationToken);

        return new UserAccessResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.IsActive,
            Convert.ToBase64String(user.Version),
            roles);
    }

    public Task<User?> GetTrackedWithRolesAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .Include(user => user.UserRoles)
            .SingleOrDefaultAsync(
                user => user.Id == userId,
                cancellationToken);
    }

    public Task<byte[]> GetVersionAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user =>
                EF.Property<byte[]>(
                    user,
                    "RowVersion"))
            .SingleAsync(cancellationToken);
    }

    public Task<List<Role>> GetRolesAsync(
        Guid[] roleIds,
        CancellationToken cancellationToken)
    {
        return dbContext.Roles
            .AsNoTracking()
            .Where(role => roleIds.Contains(role.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountActiveAdminsAsync(
        Guid adminRoleId,
        CancellationToken cancellationToken)
    {
        return dbContext.UserRoles
            .AsNoTracking()
            .Where(link =>
                link.RoleId == adminRoleId &&
                link.User.IsActive)
            .Select(link => link.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
    }
}
