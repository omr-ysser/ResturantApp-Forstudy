using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.Users.List;

public sealed record ListUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null)
    : IRequest<PagedResponse<UserSummaryResponse>>;

public sealed class ListUsersQueryValidator
    : AbstractValidator<ListUsersQuery>
{
    public ListUsersQueryValidator()
    {
        RuleFor(request => request.Page)
            .InclusiveBetween(1, 100_000);

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(request => request.Search)
            .MaximumLength(256);
    }
}

public sealed class ListUsersQueryHandler(
    IUserAccessDao userDao)
    : IRequestHandler<
        ListUsersQuery,
        PagedResponse<UserSummaryResponse>>
{
    public Task<PagedResponse<UserSummaryResponse>> Handle(
        ListUsersQuery request,
        CancellationToken cancellationToken)
    {
        return userDao.ListAsync(
            request.Page,
            request.PageSize,
            request.Search,
            request.IsActive,
            cancellationToken);
    }
}
