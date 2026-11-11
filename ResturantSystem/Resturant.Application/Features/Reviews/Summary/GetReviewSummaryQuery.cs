using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Reviews;

namespace Restaurant.Application.Features.Reviews.Summary;

public sealed record GetReviewSummaryQuery
    : IRequest<ReviewSummaryResponse>;

public sealed class GetReviewSummaryQueryHandler(
    IReviewDao reviewDao,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetReviewSummaryQuery,
        ReviewSummaryResponse>
{
    public Task<ReviewSummaryResponse> Handle(
        GetReviewSummaryQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.Access.Permissions.Contains(
                ReviewPermissions.View,
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        return reviewDao.GetSummaryAsync(
            cancellationToken);
    }
}
