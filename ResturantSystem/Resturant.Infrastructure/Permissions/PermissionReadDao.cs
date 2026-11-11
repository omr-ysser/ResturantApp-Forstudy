using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Common.Models;

namespace Resturant.Infrastructure.Permissions;

public sealed class PermissionReadDao(
    IAppDbContext dbContext)
    : IPermissionReadDao
{
    public async Task<PagedResponse<PermissionResponse>> ListAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Permissions
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();

            query = query.Where(permission =>
                permission.NormalizedKey.Contains(term) ||
                permission.DisplayName.ToUpper().Contains(term));
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var items = await query
            .OrderBy(permission => permission.NormalizedKey)
            .ThenBy(permission => permission.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(permission => new PermissionResponse(
                permission.Id,
                permission.Key,
                permission.DisplayName))
            .ToListAsync(cancellationToken);

        return new PagedResponse<PermissionResponse>(
            page,
            pageSize,
            totalCount,
            items);
    }
}