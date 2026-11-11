using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Dao;

public interface IPermissionReadDao
{
    Task<PagedResponse<PermissionResponse>> ListAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken);
}