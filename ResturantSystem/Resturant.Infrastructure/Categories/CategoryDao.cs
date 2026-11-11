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

namespace Resturant.Infrastructure.Categories;

public sealed class CategoryDao(
    IAppDbContext dbContext)
    : ICategoryDao
{
    private sealed record CategoryRow(
        Guid Id,
        string Name,
        string? Description,
        bool IsActive,
        int MenuItemsCount,
        byte[] Version);

    private static readonly Expression<
        Func<Category, CategoryRow>> Projection =
        category => new CategoryRow(
            category.Id,
            category.Name,
            category.Description,
            category.IsActive,
            category.MenuItems.Count,
            EF.Property<byte[]>(
                category,
                "RowVersion"));

    public async Task<PagedResponse<CategoryResponse>> ListAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Categories
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(category =>
                category.Name.Contains(term) ||
                (category.Description != null &&
                 category.Description.Contains(term)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(category =>
                category.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        IOrderedQueryable<Category> ordered;

        if (string.Equals(
                sortBy,
                "createdAt",
                StringComparison.OrdinalIgnoreCase))
        {
            ordered = descending
                ? query.OrderByDescending(
                    category => category.CreatedAtUtc)
                : query.OrderBy(
                    category => category.CreatedAtUtc);
        }
        else
        {
            ordered = descending
                ? query.OrderByDescending(
                    category => category.Name)
                : query.OrderBy(
                    category => category.Name);
        }

        var rows = await ordered
            .ThenBy(category => category.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(Map)
            .ToArray();

        return new PagedResponse<CategoryResponse>(
            page,
            pageSize,
            totalCount,
            items);
    }

    public async Task<CategoryResponse?> GetAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.Categories
            .AsNoTracking()
            .Where(category =>
                category.Id == categoryId)
            .Select(Projection)
            .SingleOrDefaultAsync(cancellationToken);

        return row is null ? null : Map(row);
    }

    public Task<Category?> LockForUpdateAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return dbContext.Categories
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [dbo].[Categories] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {categoryId}
                """)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<byte[]> GetVersionAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return dbContext.Categories
            .AsNoTracking()
            .Where(category =>
                category.Id == categoryId)
            .Select(category =>
                EF.Property<byte[]>(
                    category,
                    "RowVersion"))
            .SingleAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string name,
        Guid? excludingCategoryId,
        CancellationToken cancellationToken)
    {
        var trimmedName = name.Trim();

        return dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                category =>
                    category.Name == trimmedName &&
                    (!excludingCategoryId.HasValue ||
                     category.Id != excludingCategoryId.Value),
                cancellationToken);
    }

    public Task<bool> HasMenuItemsAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        return dbContext.MenuItems
            .AsNoTracking()
            .AnyAsync(
                item => item.CategoryId == categoryId,
                cancellationToken);
    }

    public void Add(Category category)
    {
        dbContext.Categories.Add(category);
    }

    public void Remove(Category category)
    {
        dbContext.Categories.Remove(category);
    }

    private static CategoryResponse Map(
        CategoryRow row)
    {
        return new CategoryResponse(
            row.Id,
            row.Name,
            row.Description,
            row.IsActive,
            row.MenuItemsCount,
            Convert.ToBase64String(row.Version));
    }
}
