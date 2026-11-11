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

namespace Restaurant.Application.Features.Reviews.GetForOrder;

public sealed record GetOrderReviewQuery(Guid OrderId)
    : IRequest<ReviewResponse>;

public sealed class GetOrderReviewQueryValidator
    : AbstractValidator<GetOrderReviewQuery>
{
    public GetOrderReviewQueryValidator()
    {
        RuleFor(query => query.OrderId)
            .NotEmpty();
    }
}

public sealed class GetOrderReviewQueryHandler(
    IReviewDao reviewDao,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetOrderReviewQuery,
        ReviewResponse>
{
    public async Task<ReviewResponse> Handle(
        GetOrderReviewQuery request,
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

        return await reviewDao.GetForOrderAsync(
                   request.OrderId,
                   currentUser.UserId,
                   canModerate,
                   cancellationToken)
               ?? throw new ResourceNotFoundException("Review");
    }
}
