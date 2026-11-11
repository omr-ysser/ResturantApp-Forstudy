using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Services;

public interface ICategoryService
{
    Task<CategoryResponse> CreateAsync(
        string name,
        string? description,
        CancellationToken cancellationToken);

    Task<CategoryResponse> UpdateAsync(
        Guid categoryId,
        string name,
        string? description,
        string version,
        CancellationToken cancellationToken);

    Task<CategoryResponse> SetStatusAsync(
        Guid categoryId,
        bool isActive,
        string version,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Guid categoryId,
        string version,
        CancellationToken cancellationToken);
}
