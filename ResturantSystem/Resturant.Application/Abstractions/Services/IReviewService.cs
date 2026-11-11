using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Services;

public interface IReviewService
{
    Task<ReviewResponse> CreateAsync(
        Guid orderId,
        int rating,
        string? comment,
        CancellationToken cancellationToken);

    Task<ReviewResponse> UpdateAsync(
        Guid reviewId,
        int rating,
        string? comment,
        string version,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Guid reviewId,
        string version,
        CancellationToken cancellationToken);

    Task<ReviewResponse> ModerateAsync(
        Guid reviewId,
        bool isHidden,
        string reason,
        string version,
        CancellationToken cancellationToken);
}