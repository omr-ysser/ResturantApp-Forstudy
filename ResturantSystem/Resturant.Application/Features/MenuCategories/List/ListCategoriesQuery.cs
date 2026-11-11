using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.MenuCategories.List;

public sealed record ListCategoriesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool? IsActive = null,
    string SortBy = "name",
    bool Descending = false)
    : IRequest<PagedResponse<CategoryResponse>>;

public sealed class ListCategoriesQueryValidator
    : AbstractValidator<ListCategoriesQuery>
{
    public ListCategoriesQueryValidator()
    {
        RuleFor(request => request.Page)
            .InclusiveBetween(1, 100_000);

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(request => request.Search)
            .MaximumLength(100);

        RuleFor(request => request.SortBy)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value =>
                string.Equals(
                    value,
                    "name",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    value,
                    "createdAt",
                    StringComparison.OrdinalIgnoreCase))
            .WithMessage(
                "SortBy must be name or createdAt.");
    }
}

public sealed class ListCategoriesQueryHandler(
    ICategoryDao categoryDao)
    : IRequestHandler<
        ListCategoriesQuery,
        PagedResponse<CategoryResponse>>
{
    public Task<PagedResponse<CategoryResponse>> Handle(
        ListCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        return categoryDao.ListAsync(
            request.Page,
            request.PageSize,
            request.Search,
            request.IsActive,
            request.SortBy,
            request.Descending,
            cancellationToken);
    }
}
