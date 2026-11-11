using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Authentication;

public sealed class UserAccountDao(
    IAppDbContext dbContext)
    : IUserAccountDao
{
    public Task<bool> EmailExistsAsync(
        string normalizedEmail,
        Guid? excludingUserId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user =>
                    user.NormalizedEmail == normalizedEmail &&
                    (!excludingUserId.HasValue ||
                     user.Id != excludingUserId.Value),
                cancellationToken);
    }

    public void Add(User user)
    {
        dbContext.Users.Add(user);
    }
}
