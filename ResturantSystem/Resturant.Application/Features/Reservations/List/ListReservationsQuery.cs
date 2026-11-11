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
using Restaurant.Application.Features.Reservations;
using Resturant.Domain.Common;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.Reservations.List;

public sealed record ListReservationsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    ReservationStatus? Status = null,
    Guid? DiningTableId = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    bool MineOnly = false,
    string SortBy = "startsAt",
    bool Descending = false)
    : IRequest<PagedResponse<ReservationResponse>>
{
    public override string ToString() =>
        nameof(ListReservationsQuery);
}

public sealed class ListReservationsQueryValidator
    : AbstractValidator<ListReservationsQuery>
{
    public ListReservationsQueryValidator()
    {
        RuleFor(query => query.Page)
            .InclusiveBetween(1, 100_000);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.Search)
            .MaximumLength(
                ReservationRules.MaximumCustomerNameLength);

        RuleFor(query => query.Status)
            .Must(status =>
                !status.HasValue ||
                Enum.IsDefined(
                    typeof(ReservationStatus),
                    status.Value))
            .WithMessage(
                "Status must be a valid reservation status.");

        RuleFor(query => query.DiningTableId)
            .Must(id =>
                !id.HasValue ||
                id.Value != Guid.Empty)
            .WithMessage(
                "DiningTableId must be a non-empty identifier.");

        RuleFor(query => query)
            .Must(query =>
                !query.FromUtc.HasValue ||
                !query.ToUtc.HasValue ||
                query.FromUtc.Value < query.ToUtc.Value)
            .WithMessage(
                "ToUtc must be later than FromUtc.")
            .OverridePropertyName(
                nameof(ListReservationsQuery.ToUtc));

        RuleFor(query => query.SortBy)
            .Must(sortBy =>
                string.Equals(
                    sortBy,
                    "startsAt",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    sortBy,
                    "createdAt",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    sortBy,
                    "tableNumber",
                    StringComparison.OrdinalIgnoreCase))
            .WithMessage(
                "SortBy must be startsAt, createdAt, or tableNumber.");
    }
}

public sealed class ListReservationsQueryHandler(
    IReservationDao reservationDao,
    ICurrentUser currentUser)
    : IRequestHandler<
        ListReservationsQuery,
        PagedResponse<ReservationResponse>>
{
    public Task<PagedResponse<ReservationResponse>> Handle(
        ListReservationsQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = currentUser.Access.Permissions;

        var canViewAll = permissions.Contains(
            ReservationPermissions.ViewAll,
            StringComparer.Ordinal);

        var canViewOwn = permissions.Contains(
            ReservationPermissions.ViewOwn,
            StringComparer.Ordinal);

        if (!canViewAll && !canViewOwn)
        {
            throw new ForbiddenOperationException();
        }

        Guid? customerScope =
            canViewAll && !request.MineOnly
                ? null
                : currentUser.UserId;

        var filter = new ReservationListFilter(
            request.Page,
            request.PageSize,
            request.Search,
            request.Status,
            request.DiningTableId,
            request.FromUtc,
            request.ToUtc,
            request.SortBy,
            request.Descending);

        return reservationDao.ListAsync(
            filter,
            customerScope,
            cancellationToken);
    }
}
