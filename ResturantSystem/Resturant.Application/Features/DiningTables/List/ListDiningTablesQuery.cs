using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.DiningTables.List;

public sealed record ListDiningTablesQuery(
    int Page = 1,
    int PageSize = 20,
    int? Number = null,
    int? MinimumCapacity = null,
    TableStatus? Status = null,
    bool? CanAcceptOrder = null,
    string SortBy = "number",
    bool Descending = false)
    : IRequest<PagedResponse<DiningTableResponse>>;

public sealed class ListDiningTablesQueryValidator
    : AbstractValidator<ListDiningTablesQuery>
{
    public ListDiningTablesQueryValidator()
    {
        RuleFor(query => query.Page)
            .InclusiveBetween(1, 100_000);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.Number)
            .Must(number =>
                !number.HasValue ||
                number.Value > 0)
            .WithMessage(
                "Number must be positive.");

        RuleFor(query => query.MinimumCapacity)
            .Must(capacity =>
                !capacity.HasValue ||
                capacity.Value > 0)
            .WithMessage(
                "MinimumCapacity must be positive.");

        RuleFor(query => query.Status)
            .Must(status =>
                !status.HasValue ||
                Enum.IsDefined(
                    typeof(TableStatus),
                    status.Value))
            .WithMessage(
                "Status must be a valid table status.");

        RuleFor(query => query.SortBy)
            .Must(sortBy =>
                string.Equals(
                    sortBy,
                    "number",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    sortBy,
                    "capacity",
                    StringComparison.OrdinalIgnoreCase))
            .WithMessage(
                "SortBy must be number or capacity.");
    }
}

public sealed class ListDiningTablesQueryHandler(
    IDiningTableDao diningTableDao)
    : IRequestHandler<
        ListDiningTablesQuery,
        PagedResponse<DiningTableResponse>>
{
    public Task<PagedResponse<DiningTableResponse>> Handle(
        ListDiningTablesQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new DiningTableListFilter(
            request.Page,
            request.PageSize,
            request.Number,
            request.MinimumCapacity,
            request.Status,
            request.CanAcceptOrder,
            request.SortBy,
            request.Descending);

        return diningTableDao.ListAsync(
            filter,
            cancellationToken);
    }
}
