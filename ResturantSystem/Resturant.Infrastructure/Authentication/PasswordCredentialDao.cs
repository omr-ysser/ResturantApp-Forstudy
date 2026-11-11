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

public sealed class PasswordCredentialDao(
    IAppDbContext dbContext)
    : IPasswordCredentialDao
{
    public Task<LoginUser?> FindAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new LoginUser(
                user.Id,
                user.PasswordHash,
                user.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
