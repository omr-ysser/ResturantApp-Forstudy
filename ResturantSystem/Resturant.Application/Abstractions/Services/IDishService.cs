using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Services;

public interface IDishService
{
    Task<DishResponse> CreateAsync(
        Guid categoryId,
        string name,
        decimal price,
        string? description,
        CancellationToken cancellationToken);

    Task<DishResponse> UpdateAsync(
        Guid id,
        string name,
        decimal price,
        string? description,
        string version,
        CancellationToken cancellationToken);

    Task<DishResponse> SetAvailabilityAsync(
        Guid id,
        bool isAvailable,
        string version,
        CancellationToken cancellationToken);
}
