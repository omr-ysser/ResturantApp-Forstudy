using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Reviews;
using Resturant.Domain.Common;

namespace Restaurant.Application.Features.Reviews.List;

public sealed record ListReviewsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    int? Rating = null,
    bool MineOnly = false,
    ReviewVisibility Visibility = ReviewVisibility.Visible,
    string SortBy = "createdAt",
    bool Descending = true)
    : IRequest<PagedResponse<ReviewResponse>>
{
    public override string ToString() =>
        nameof(ListReviewsQuery);
}

public sealed class ListReviewsQueryValidator
    : AbstractValidator<ListReviewsQuery>
{
    public ListReviewsQueryValidator()
    {
        RuleFor(query => query.Page)
            .InclusiveBetween(1, 100_000);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.Search)
            .MaximumLength(200);

        RuleFor(query => query.Rating)
            .Must(rating =>
                !rating.HasValue ||
                (
                    rating.Value >= ReviewRules.MinimumRating &&
                    rating.Value <= ReviewRules.MaximumRating
                ))
            .WithMessage(
                "Rating must be between 1 and 5.");

        RuleFor(query => query.Visibility)
            .Must(visibility =>
                Enum.IsDefined(
                    typeof(ReviewVisibility),
                    visibility))
            .WithMessage(
                "Visibility must be Visible, Hidden, or All.");

        RuleFor(query => query.SortBy)
            .Must(sortBy =>
                string.Equals(
                    sortBy,
                    "createdAt",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    sortBy,
                    "rating",
                    StringComparison.OrdinalIgnoreCase))
            .WithMessage(
                "SortBy must be createdAt or rating.");
    }
}

public sealed class ListReviewsQueryHandler(
    IReviewDao reviewDao,
    ICurrentUser currentUser)
    : IRequestHandler<
        ListReviewsQuery,
        PagedResponse<ReviewResponse>>
{
    public Task<PagedResponse<ReviewResponse>> Handle(
        ListReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = currentUser.Access.Permissions;

        if (!permissions.Contains(
                ReviewPermissions.View,
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        var canModerate = permissions.Contains(
            ReviewPermissions.Moderate,
            StringComparer.Ordinal);

        var requestsHiddenRecords =
            request.Visibility != ReviewVisibility.Visible;

        if (requestsHiddenRecords &&
            !request.MineOnly &&
            !canModerate)
        {
            throw new ForbiddenOperationException();
        }

        var filter = new ReviewListFilter(
            request.Page,
            request.PageSize,
            request.Search,
            request.Rating,
            request.MineOnly,
            request.Visibility,
            request.SortBy,
            request.Descending);

        return reviewDao.ListAsync(
            filter,
            currentUser.UserId,
            canModerate,
            cancellationToken);
    }
}
