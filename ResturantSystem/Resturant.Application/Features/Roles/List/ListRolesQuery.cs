using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.Roles.List;

public sealed record ListRolesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null)
    : IRequest<PagedResponse<RoleSummaryResponse>>;

public sealed class ListRolesQueryValidator
    : AbstractValidator<ListRolesQuery>
{
    public ListRolesQueryValidator()
    {
        RuleFor(request => request.Page)
            .InclusiveBetween(1, 100_000);

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(request => request.Search)
            .MaximumLength(100);
    }
}

public sealed class ListRolesQueryHandler(
    IRolePermissionDao rolePermissionDao)
    : IRequestHandler<
        ListRolesQuery,
        PagedResponse<RoleSummaryResponse>>
{
    public Task<PagedResponse<RoleSummaryResponse>> Handle(
        ListRolesQuery request,
        CancellationToken cancellationToken)
    {
        return rolePermissionDao.ListRolesAsync(
            request.Page,
            request.PageSize,
            request.Search,
            cancellationToken);
    }
}
