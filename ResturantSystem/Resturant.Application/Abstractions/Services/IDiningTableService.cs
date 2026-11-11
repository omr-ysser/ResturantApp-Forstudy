using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Services;

public interface IDiningTableService
{
    Task<DiningTableResponse> CreateAsync(
        int number,
        int capacity,
        CancellationToken cancellationToken);

    Task<DiningTableResponse> UpdateAsync(
        Guid id,
        int number,
        int capacity,
        string version,
        CancellationToken cancellationToken);

    Task<DiningTableResponse> SetServiceStateAsync(
        Guid id,
        bool isInService,
        string version,
        CancellationToken cancellationToken);
}