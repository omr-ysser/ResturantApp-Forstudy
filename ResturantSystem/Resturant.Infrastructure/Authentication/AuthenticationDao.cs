using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Common.Models;

namespace Resturant.Infrastructure.Authentication;

public sealed class AuthenticationDao(
    IAppDbContext dbContext)
    : IAuthenticationDao
{
    public Task<LoginUser?> FindLoginUserAsync(
        string normalizedEmail,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AsNoTracking()
            .Where(user =>
                user.NormalizedEmail == normalizedEmail)
            .Select(user => new LoginUser(
                user.Id,
                user.PasswordHash,
                user.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AccessProfile?> GetAccessAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(user =>
                user.Id == userId &&
                user.IsActive)
            .Select(user => new
            {
                user.Id,
                user.FullName,
                user.Email
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .SelectMany(user => user.UserRoles)
            .Select(userRole => userRole.Role.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        var permissions = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .SelectMany(user => user.UserRoles)
            .SelectMany(userRole =>
                userRole.Role.RolePermissions)
            .Select(rolePermission =>
                rolePermission.Permission.Key)
            .Distinct()
            .OrderBy(key => key)
            .ToListAsync(cancellationToken);

        return new AccessProfile(
            user.Id,
            user.FullName,
            user.Email,
            roles,
            permissions);
    }
}
