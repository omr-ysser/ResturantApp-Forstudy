using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Dishes;

public sealed class DishManagementDao(
    IAppDbContext dbContext)
    : IDishManagementDao
{
    public Task<MenuItem?> GetTrackedAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return dbContext.MenuItems
            .AsTracking()
            .SingleOrDefaultAsync(
                item => item.Id == id,
                cancellationToken);
    }

    public void SetExpectedVersion(
        MenuItem dish,
        byte[] expectedVersion)
    {
        dbContext.MenuItems
            .Entry(dish)
            .Property<byte[]>("RowVersion")
            .OriginalValue = expectedVersion;
    }

    public Task<bool> NameExistsAsync(
        Guid categoryId,
        string name,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        var trimmedName = name.Trim();

        var query = dbContext.MenuItems
            .AsNoTracking()
            .Where(item =>
                item.CategoryId == categoryId &&
                item.Name == trimmedName);

        if (excludingId.HasValue)
        {
            var excludedId = excludingId.Value;

            query = query.Where(item =>
                item.Id != excludedId);
        }

        return query.AnyAsync(cancellationToken);
    }

    public void Add(MenuItem dish)
    {
        dbContext.MenuItems.Add(dish);
    }
}
