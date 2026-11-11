using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Dao;

public interface IReviewDao
{
    Task<PagedResponse<ReviewResponse>> ListAsync(
        ReviewListFilter filter,
        Guid viewerUserId,
        bool canModerate,
        CancellationToken cancellationToken);

    Task<ReviewSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken);

    Task<bool> ExistsForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken);

    Task<ReviewResponse?> GetForOrderAsync(
        Guid orderId,
        Guid viewerUserId,
        bool canModerate,
        CancellationToken cancellationToken);

    Task<Review?> GetTrackedOwnedForUpdateAsync(
        Guid reviewId,
        Guid authorUserId,
        CancellationToken cancellationToken);

    Task<Review?> GetTrackedForModerationAsync(
        Guid reviewId,
        CancellationToken cancellationToken);

    byte[] GetVersion(Review review);

    void SetExpectedVersion(
        Review review,
        byte[] version);

    void Add(Review review);
}