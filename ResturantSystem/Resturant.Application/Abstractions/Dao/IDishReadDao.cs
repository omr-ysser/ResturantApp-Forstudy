using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Dao;

public interface IDishReadDao
{
    Task<PagedResponse<DishResponse>> ListAsync(
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
        CancellationToken cancellationToken);

    Task<DishResponse?> GetAsync(
        Guid id,
        CancellationToken cancellationToken);
}
