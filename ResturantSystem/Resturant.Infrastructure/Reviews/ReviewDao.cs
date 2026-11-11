using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Reviews;

public sealed class ReviewDao(
    IAppDbContext dbContext)
    : IReviewDao
{
    public async Task<PagedResponse<ReviewResponse>> ListAsync(
        ReviewListFilter filter,
        Guid viewerUserId,
        bool canModerate,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Reviews
            .AsNoTracking()
            .Where(review =>
                !review.IsDeleted &&
                (
                    !review.IsHidden ||
                    review.AuthorUserId == viewerUserId ||
                    canModerate
                ));

        if (filter.MineOnly)
        {
            query = query.Where(review =>
                review.AuthorUserId == viewerUserId);
        }

        query = filter.Visibility switch
        {
            ReviewVisibility.Visible =>
                query.Where(review => !review.IsHidden),

            ReviewVisibility.Hidden =>
                query.Where(review => review.IsHidden),

            ReviewVisibility.All => query,

            _ => throw new ArgumentOutOfRangeException(
                nameof(filter.Visibility),
                "Unsupported review visibility.")
        };

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();

            query = query.Where(review =>
                review.Comment != null &&
                review.Comment.Contains(search));
        }

        if (filter.Rating.HasValue)
        {
            var rating = filter.Rating.Value;

            query = query.Where(review =>
                review.Rating == rating);
        }

        // Count only records inside the authorized visibility scope.
        var totalCount = await query.CountAsync(
            cancellationToken);

        var orderedQuery = filter.SortBy
            .ToLowerInvariant() switch
        {
            "createdat" => filter.Descending
                ? query.OrderByDescending(
                    review => review.CreatedAtUtc)
                : query.OrderBy(
                    review => review.CreatedAtUtc),

            "rating" => filter.Descending
                ? query.OrderByDescending(
                    review => review.Rating)
                : query.OrderBy(
                    review => review.Rating),

            _ => throw new ArgumentOutOfRangeException(
                nameof(filter.SortBy),
                "Unsupported review sorting field.")
        };

        var rows = await orderedQuery
            .ThenBy(review => review.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(review => new
            {
                review.Id,
                review.OrderId,
                review.Rating,
                review.Comment,
                review.IsHidden,
                review.AuthorUserId,
                review.CreatedAtUtc,
                review.UpdatedAtUtc,

                Version = EF.Property<byte[]>(
                    review,
                    "RowVersion")
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(review => new ReviewResponse(
                review.Id,
                review.OrderId,
                review.Rating,
                review.Comment,
                review.IsHidden,
                review.AuthorUserId == viewerUserId,
                review.CreatedAtUtc,
                review.UpdatedAtUtc,
                Convert.ToBase64String(review.Version)))
            .ToArray();

        return new PagedResponse<ReviewResponse>(
            filter.Page,
            filter.PageSize,
            totalCount,
            items);
    }

    public async Task<ReviewSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken)
    {
        var groups = await dbContext.Reviews
            .AsNoTracking()
            .Where(review =>
                !review.IsDeleted &&
                !review.IsHidden)
            .GroupBy(review => review.Rating)
            .Select(group => new
            {
                Rating = group.Key,
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        // SQL returns at most five groups.
        var countByRating = groups.ToDictionary(
            group => group.Rating,
            group => group.Count);

        var totalReviews = groups.Sum(
            group => group.Count);

        var weightedTotal = groups.Sum(
            group => (decimal)group.Rating * group.Count);

        decimal? averageRating = totalReviews == 0
            ? null
            : decimal.Round(
                weightedTotal / totalReviews,
                2,
                MidpointRounding.AwayFromZero);

        var distribution = Enumerable.Range(1, 5)
            .Select(rating => new ReviewRatingCount(
                rating,
                countByRating.GetValueOrDefault(rating)))
            .ToArray();

        return new ReviewSummaryResponse(
            totalReviews,
            averageRating,
            distribution);
    }

    public Task<bool> ExistsForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        return dbContext.Reviews
            .AsNoTracking()
            .AnyAsync(
                review => review.OrderId == orderId,
                cancellationToken);
    }

    public async Task<ReviewResponse?> GetForOrderAsync(
        Guid orderId,
        Guid viewerUserId,
        bool canModerate,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.Reviews
            .AsNoTracking()
            .Where(review =>
                review.OrderId == orderId &&
                !review.IsDeleted &&
                (
                    !review.IsHidden ||
                    review.AuthorUserId == viewerUserId ||
                    canModerate
                ))
            .Select(review => new
            {
                review.Id,
                review.OrderId,
                review.Rating,
                review.Comment,
                review.IsHidden,
                review.AuthorUserId,
                review.CreatedAtUtc,
                review.UpdatedAtUtc,

                Version = EF.Property<byte[]>(
                    review,
                    "RowVersion")
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        return new ReviewResponse(
            row.Id,
            row.OrderId,
            row.Rating,
            row.Comment,
            row.IsHidden,
            row.AuthorUserId == viewerUserId,
            row.CreatedAtUtc,
            row.UpdatedAtUtc,
            Convert.ToBase64String(row.Version));
    }

    public Task<Review?> GetTrackedOwnedForUpdateAsync(
        Guid reviewId,
        Guid authorUserId,
        CancellationToken cancellationToken)
    {
        return dbContext.Reviews
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [dbo].[Reviews] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {reviewId}
                  AND [AuthorUserId] = {authorUserId}
                  AND [IsDeleted] = 0
                """)
            .AsTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<Review?> GetTrackedForModerationAsync(
        Guid reviewId,
        CancellationToken cancellationToken)
    {
        return dbContext.Reviews
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [dbo].[Reviews] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {reviewId}
                  AND [IsDeleted] = 0
                """)
            .AsTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public byte[] GetVersion(Review review)
    {
        var version = dbContext.Reviews
            .Entry(review)
            .Property<byte[]>("RowVersion")
            .CurrentValue;

        if (version is null || version.Length != 8)
        {
            throw new InvalidOperationException(
                "A persisted review rowversion is required.");
        }

        return version;
    }

    public void SetExpectedVersion(
        Review review,
        byte[] version)
    {
        dbContext.Reviews
            .Entry(review)
            .Property<byte[]>("RowVersion")
            .OriginalValue = version;
    }

    public void Add(Review review)
    {
        dbContext.Reviews.Add(review);
    }
}