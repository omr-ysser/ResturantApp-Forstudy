using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Dao;

public interface ICategoryDao
{
    Task<PagedResponse<CategoryResponse>> ListAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken);

    Task<CategoryResponse?> GetAsync(
        Guid categoryId,
        CancellationToken cancellationToken);

    Task<Category?> LockForUpdateAsync(
        Guid categoryId,
        CancellationToken cancellationToken);

    Task<byte[]> GetVersionAsync(
        Guid categoryId,
        CancellationToken cancellationToken);

    Task<bool> NameExistsAsync(
        string name,
        Guid? excludingCategoryId,
        CancellationToken cancellationToken);

    Task<bool> HasMenuItemsAsync(
        Guid categoryId,
        CancellationToken cancellationToken);

    void Add(Category category);

    void Remove(Category category);
}
