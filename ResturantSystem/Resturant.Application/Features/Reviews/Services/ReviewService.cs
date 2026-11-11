using Microsoft.Extensions.Logging;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Common.Security;
using Restaurant.Application.Features.Reviews;
using Resturant.Domain.Entities;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.Reviews.Services;

public sealed class ReviewService(
    IReviewDao reviewDao,
    IOrderWorkflowDao orderWorkflowDao,
    IAuthenticationDao authenticationDao,
    IAuthenticationSessionDao sessionDao,
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    ILogger<ReviewService> logger)
    : IReviewService
{
    public async Task<ReviewResponse> CreateAsync(
        Guid orderId,
        int rating,
        string? comment,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var authorId = await GetAuthorizedActorAsync(
            ReviewPermissions.CreateOwn,
            cancellationToken);

        var order =
            await orderWorkflowDao.GetTrackedForUpdateAsync(
                orderId,
                cancellationToken);

        if (order is null ||
            order.CustomerUserId != authorId)
        {
            throw new ResourceNotFoundException("Order");
        }

        if (order.Status != OrderStatus.Completed)
        {
            throw new BusinessConflictException(
                "Only completed orders can be reviewed.");
        }

        if (await reviewDao.ExistsForOrderAsync(
                order.Id,
                cancellationToken))
        {
            throw new BusinessConflictException(
                "An order can be reviewed only once, " +
                "including reviews that were hidden or withdrawn.");
        }

        var review = new Review(
            order,
            authorId,
            rating,
            comment);

        reviewDao.Add(review);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredResponseAsync(
            review.OrderId,
            authorId,
            canModerate: false,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Review {ReviewId} created for order {OrderId} by user {UserId}",
            review.Id,
            order.Id,
            authorId);

        return response;
    }

    public async Task<ReviewResponse> UpdateAsync(
        Guid reviewId,
        int rating,
        string? comment,
        string version,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var authorId = await GetAuthorizedActorAsync(
            ReviewPermissions.EditOwn,
            cancellationToken);

        var review = await GetOwnedForMutationAsync(
            reviewId,
            authorId,
            version,
            cancellationToken);

        review.UpdateContent(
            authorId,
            rating,
            comment);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredResponseAsync(
            review.OrderId,
            authorId,
            canModerate: false,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Review {ReviewId} updated by user {UserId}",
            review.Id,
            authorId);

        return response;
    }

    public async Task DeleteAsync(
        Guid reviewId,
        string version,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var authorId = await GetAuthorizedActorAsync(
            ReviewPermissions.DeleteOwn,
            cancellationToken);

        var review = await GetOwnedForMutationAsync(
            reviewId,
            authorId,
            version,
            cancellationToken);

        review.Delete(
            authorId,
            timeProvider.GetUtcNow());

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Review {ReviewId} withdrawn by user {UserId}",
            review.Id,
            authorId);
    }

    public async Task<ReviewResponse> ModerateAsync(
        Guid reviewId,
        bool isHidden,
        string reason,
        string version,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var moderatorId = await GetAuthorizedActorAsync(
            ReviewPermissions.Moderate,
            cancellationToken);

        var review =
            await reviewDao.GetTrackedForModerationAsync(
                reviewId,
                cancellationToken);

        if (review is null)
        {
            throw new ResourceNotFoundException("Review");
        }

        EnsureExpectedVersion(
            review,
            version);

        if (review.IsHidden == isHidden)
        {
            throw new BusinessConflictException(
                "The review already has the requested visibility.");
        }

        review.SetHidden(
            isHidden,
            moderatorId,
            reason,
            timeProvider.GetUtcNow());

        await dbContext.SaveChangesAsync(
            cancellationToken);

        var response = await GetRequiredResponseAsync(
            review.OrderId,
            moderatorId,
            canModerate: true,
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        logger.LogInformation(
            "Review {ReviewId} visibility changed to IsHidden {IsHidden} by user {UserId}",
            review.Id,
            isHidden,
            moderatorId);

        return response;
    }

    private async Task<Guid> GetAuthorizedActorAsync(
        string requiredPermission,
        CancellationToken cancellationToken)
    {
        var actorId = currentUser.UserId;

        // Must run inside the workflow transaction.
        await sessionDao.LockUserAsync(
            actorId,
            cancellationToken);

        var access = await authenticationDao.GetAccessAsync(
            actorId,
            cancellationToken);

        if (access is null ||
            !await sessionDao.IsSessionActiveAsync(
                actorId,
                currentUser.SessionId,
                timeProvider.GetUtcNow(),
                cancellationToken))
        {
            throw new AuthenticationFailedException();
        }

        if (!access.Permissions.Contains(
                requiredPermission,
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        return actorId;
    }

    private async Task<Review> GetOwnedForMutationAsync(
        Guid reviewId,
        Guid authorId,
        string version,
        CancellationToken cancellationToken)
    {
        var review =
            await reviewDao.GetTrackedOwnedForUpdateAsync(
                reviewId,
                authorId,
                cancellationToken);

        if (review is null)
        {
            throw new ResourceNotFoundException("Review");
        }

        EnsureExpectedVersion(
            review,
            version);

        return review;
    }

    private void EnsureExpectedVersion(
        Review review,
        string version)
    {
        if (!RowVersionFormat.IsValid(version))
        {
            throw new BusinessConflictException(
                "The review version is invalid. Reload the review.");
        }

        var expectedVersion = Convert.FromBase64String(
            version);

        var actualVersion = reviewDao.GetVersion(review);

        if (!actualVersion.AsSpan().SequenceEqual(
                expectedVersion))
        {
            throw new BusinessConflictException(
                "The review has changed. Reload it and try again.");
        }

        reviewDao.SetExpectedVersion(
            review,
            expectedVersion);
    }

    private async Task<ReviewResponse> GetRequiredResponseAsync(
        Guid orderId,
        Guid viewerUserId,
        bool canModerate,
        CancellationToken cancellationToken)
    {
        return await reviewDao.GetForOrderAsync(
                   orderId,
                   viewerUserId,
                   canModerate,
                   cancellationToken)
               ?? throw new ResourceNotFoundException("Review");
    }
}