using FluentValidation;
using MediatR;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.Permissions.List;

public sealed record ListPermissionsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null)
    : IRequest<PagedResponse<PermissionResponse>>;

public sealed class ListPermissionsQueryValidator
    : AbstractValidator<ListPermissionsQuery>
{
    public ListPermissionsQueryValidator()
    {
        RuleFor(request => request.Page)
            .InclusiveBetween(1, 100_000);

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(request => request.Search)
            .MaximumLength(150);
    }
}