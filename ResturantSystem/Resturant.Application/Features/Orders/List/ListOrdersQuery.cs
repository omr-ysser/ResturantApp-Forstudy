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
using Resturant.Domain.Common;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.Orders.List;

public sealed record ListOrdersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    OrderStatus? Status = null,
    OrderType? Type = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    decimal? MinTotal = null,
    decimal? MaxTotal = null,
    string SortBy = "createdAt",
    bool Descending = true,
    bool MineOnly = false)
    : IRequest<PagedResponse<OrderListItemResponse>>;

public sealed class ListOrdersQueryValidator
    : AbstractValidator<ListOrdersQuery>
{
    public ListOrdersQueryValidator()
    {
        RuleFor(query => query.Page)
            .InclusiveBetween(1, 100_000);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.Search)
            .MaximumLength(
                OrderRules.MaximumOrderNumberLength);

        RuleFor(query => query.Status)
            .Must(status =>
                !status.HasValue ||
                Enum.IsDefined(
                    typeof(OrderStatus),
                    status.Value))
            .WithMessage(
                "Status must be a valid order status.");

        RuleFor(query => query.Type)
            .Must(type =>
                !type.HasValue ||
                Enum.IsDefined(
                    typeof(OrderType),
                    type.Value))
            .WithMessage(
                "Type must be a valid order type.");

        RuleFor(query => query)
            .Must(query =>
                !query.FromUtc.HasValue ||
                !query.ToUtc.HasValue ||
                query.FromUtc.Value < query.ToUtc.Value)
            .WithMessage(
                "ToUtc must be later than FromUtc.")
            .OverridePropertyName(
                nameof(ListOrdersQuery.ToUtc));

        RuleFor(query => query.MinTotal)
            .Must(IsValidMoneyFilter)
            .WithMessage(
                "MinTotal must be non-negative, fit decimal(18,2), " +
                "and contain at most two decimal places.");

        RuleFor(query => query.MaxTotal)
            .Must(IsValidMoneyFilter)
            .WithMessage(
                "MaxTotal must be non-negative, fit decimal(18,2), " +
                "and contain at most two decimal places.");

        RuleFor(query => query)
            .Must(query =>
                !query.MinTotal.HasValue ||
                !query.MaxTotal.HasValue ||
                query.MinTotal.Value <= query.MaxTotal.Value)
            .WithMessage(
                "MinTotal must not exceed MaxTotal.")
            .OverridePropertyName(
                nameof(ListOrdersQuery.MaxTotal));

        RuleFor(query => query.SortBy)
            .Must(IsAllowedSort)
            .WithMessage(
                "SortBy must be createdAt, orderNumber, or total.");
    }

    private static bool IsValidMoneyFilter(decimal? value)
    {
        if (!value.HasValue)
        {
            return true;
        }

        var amount = value.Value;

        return amount >= 0 &&
               amount <= OrderRules.MaximumMoneyAmount &&
               decimal.Round(amount, 2) == amount;
    }

    private static bool IsAllowedSort(string? value)
    {
        return string.Equals(
                   value,
                   "createdAt",
                   StringComparison.OrdinalIgnoreCase)
               ||
               string.Equals(
                   value,
                   "orderNumber",
                   StringComparison.OrdinalIgnoreCase)
               ||
               string.Equals(
                   value,
                   "total",
                   StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ListOrdersQueryHandler(
    IOrderDao orderDao,
    ICurrentUser currentUser)
    : IRequestHandler<
        ListOrdersQuery,
        PagedResponse<OrderListItemResponse>>
{
    public Task<PagedResponse<OrderListItemResponse>> Handle(
        ListOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = currentUser.Access.Permissions;

        var canViewAll = permissions.Contains(
            "Orders.ViewAll",
            StringComparer.Ordinal);

        var canViewOwn = permissions.Contains(
            "Orders.ViewOwn",
            StringComparer.Ordinal);

        if (!canViewAll && !canViewOwn)
        {
            throw new ForbiddenOperationException();
        }

        Guid? customerScope =
            canViewAll && !request.MineOnly
                ? null
                : currentUser.UserId;

        var filter = new OrderListFilter(
            request.Page,
            request.PageSize,
            request.Search,
            request.Status,
            request.Type,
            request.FromUtc,
            request.ToUtc,
            request.MinTotal,
            request.MaxTotal,
            request.SortBy,
            request.Descending);

        return orderDao.ListAsync(
            filter,
            customerScope,
            cancellationToken);
    }
}
