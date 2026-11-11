using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.Dishes.List;

public sealed record ListDishesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? CategoryId = null,
    bool? IsAvailable = null,
    bool? IsOrderable = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string SortBy = "name",
    bool Descending = false)
    : IRequest<PagedResponse<DishResponse>>;

public sealed class ListDishesQueryValidator
    : AbstractValidator<ListDishesQuery>
{
    private const decimal MaximumPrice =
        9_999_999_999_999_999.99m;

    public ListDishesQueryValidator()
    {
        RuleFor(query => query.Page)
            .InclusiveBetween(1, 100_000);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.Search)
            .MaximumLength(150);

        RuleFor(query => query.CategoryId)
            .Must(categoryId =>
                !categoryId.HasValue ||
                categoryId.Value != Guid.Empty)
            .WithMessage(
                "CategoryId must be a non-empty identifier.");

        RuleFor(query => query.MinPrice)
            .Must(IsValidPriceFilter)
            .WithMessage(
                "MinPrice must be non-negative, fit decimal(18,2), " +
                "and contain at most two decimal places.");

        RuleFor(query => query.MaxPrice)
            .Must(IsValidPriceFilter)
            .WithMessage(
                "MaxPrice must be non-negative, fit decimal(18,2), " +
                "and contain at most two decimal places.");

        RuleFor(query => query)
            .Must(query =>
                !query.MinPrice.HasValue ||
                !query.MaxPrice.HasValue ||
                query.MinPrice.Value <= query.MaxPrice.Value)
            .WithMessage(
                "MinPrice must not exceed MaxPrice.")
            .OverridePropertyName(nameof(ListDishesQuery.MaxPrice));

        RuleFor(query => query.SortBy)
            .Must(IsAllowedSort)
            .WithMessage(
                "SortBy must be name, price, or createdAt.");
    }

    private static bool IsValidPriceFilter(decimal? value)
    {
        if (!value.HasValue)
        {
            return true;
        }

        var price = value.Value;

        return price >= 0 &&
               price <= MaximumPrice &&
               decimal.Round(price, 2) == price;
    }

    private static bool IsAllowedSort(string? value)
    {
        return string.Equals(
                   value,
                   "name",
                   StringComparison.OrdinalIgnoreCase)
               ||
               string.Equals(
                   value,
                   "price",
                   StringComparison.OrdinalIgnoreCase)
               ||
               string.Equals(
                   value,
                   "createdAt",
                   StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ListDishesQueryHandler(
    IDishReadDao dishReadDao)
    : IRequestHandler<
        ListDishesQuery,
        PagedResponse<DishResponse>>
{
    public Task<PagedResponse<DishResponse>> Handle(
        ListDishesQuery request,
        CancellationToken cancellationToken)
    {
        return dishReadDao.ListAsync(
            request.Page,
            request.PageSize,
            request.Search,
            request.CategoryId,
            request.IsAvailable,
            request.IsOrderable,
            request.MinPrice,
            request.MaxPrice,
            request.SortBy,
            request.Descending,
            cancellationToken);
    }
}
