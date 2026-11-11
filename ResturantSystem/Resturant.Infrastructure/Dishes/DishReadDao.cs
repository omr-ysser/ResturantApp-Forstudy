using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Dishes;

public sealed class DishReadDao(
    IAppDbContext dbContext)
    : IDishReadDao
{
    private const string Currency = "EGP";

    private sealed record DishRow(
        Guid Id,
        Guid CategoryId,
        string CategoryName,
        string Name,
        string? Description,
        decimal Price,
        bool IsAvailable,
        bool IsCategoryActive,
        byte[] Version);

    private static readonly Expression<Func<MenuItem, DishRow>>
        Projection = item => new DishRow(
            item.Id,
            item.CategoryId,
            item.Category.Name,
            item.Name,
            item.Description,
            item.Price,
            item.IsAvailable,
            item.Category.IsActive,
            EF.Property<byte[]>(item, "RowVersion"));

    public async Task<PagedResponse<DishResponse>> ListAsync(
        int page,
        int pageSize,
        string? search,
        Guid? categoryId,
        bool? isAvailable,
        bool? isOrderable,
        decimal? minPrice,
        decimal? maxPrice,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken)
    {
        var query = dbContext.MenuItems
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(item =>
                item.Name.Contains(term) ||
                (
                    item.Description != null &&
                    item.Description.Contains(term)
                ));
        }

        if (categoryId.HasValue)
        {
            var selectedCategoryId = categoryId.Value;

            query = query.Where(item =>
                item.CategoryId == selectedCategoryId);
        }

        if (isAvailable.HasValue)
        {
            var available = isAvailable.Value;

            query = query.Where(item =>
                item.IsAvailable == available);
        }

        if (isOrderable.HasValue)
        {
            if (isOrderable.Value)
            {
                query = query.Where(item =>
                    item.IsAvailable &&
                    item.Category.IsActive);
            }
            else
            {
                query = query.Where(item =>
                    !item.IsAvailable ||
                    !item.Category.IsActive);
            }
        }

        if (minPrice.HasValue)
        {
            var minimum = minPrice.Value;

            query = query.Where(item =>
                item.Price >= minimum);
        }

        if (maxPrice.HasValue)
        {
            var maximum = maxPrice.Value;

            query = query.Where(item =>
                item.Price <= maximum);
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        IOrderedQueryable<MenuItem> orderedQuery =
            sortBy.ToLowerInvariant() switch
            {
                "name" => descending
                    ? query.OrderByDescending(item => item.Name)
                    : query.OrderBy(item => item.Name),

                "price" => descending
                    ? query.OrderByDescending(item => item.Price)
                    : query.OrderBy(item => item.Price),

                "createdat" => descending
                    ? query.OrderByDescending(
                        item => item.CreatedAtUtc)
                    : query.OrderBy(
                        item => item.CreatedAtUtc),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(sortBy),
                    "Unsupported dish sorting field.")
            };

        var rows = await orderedQuery
            .ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(Map)
            .ToArray();

        return new PagedResponse<DishResponse>(
            page,
            pageSize,
            totalCount,
            items);
    }

    public async Task<DishResponse?> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.MenuItems
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(Projection)
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : Map(row);
    }

    private static DishResponse Map(DishRow row)
    {
        return new DishResponse(
            row.Id,
            row.CategoryId,
            row.CategoryName,
            row.Name,
            row.Description,
            row.Price,
            Currency,
            row.IsAvailable,
            row.IsCategoryActive,
            row.IsAvailable && row.IsCategoryActive,
            Convert.ToBase64String(row.Version));
    }
}
