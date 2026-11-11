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

namespace Restaurant.Application.Features.Reservations.Availability;

public sealed record GetReservationAvailabilityQuery(
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    int GuestCount,
    int Page = 1,
    int PageSize = 20)
    : IRequest<PagedResponse<AvailableReservationTableResponse>>;

public sealed class GetReservationAvailabilityQueryValidator
    : AbstractValidator<GetReservationAvailabilityQuery>
{
    public GetReservationAvailabilityQueryValidator(
        TimeProvider timeProvider)
    {
        RuleFor(query => query.Page)
            .InclusiveBetween(1, 100_000);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.GuestCount)
            .InclusiveBetween(
                1,
                ReservationRules.MaximumGuestCount);

        RuleFor(query => query.StartsAtUtc)
            .NotEmpty()
            .Must(start =>
            {
                var leadTime =
                    start - timeProvider.GetUtcNow();

                return leadTime >= ReservationRules.MinimumLeadTime &&
                       leadTime <= ReservationRules.MaximumAdvance;
            })
            .WithMessage(
                "The reservation must start at least 15 minutes " +
                "and no more than 30 days from now.");

        RuleFor(query => query.EndsAtUtc)
            .NotEmpty();

        RuleFor(query => query)
            .Must(query =>
            {
                var duration =
                    query.EndsAtUtc - query.StartsAtUtc;

                return duration >= ReservationRules.MinimumDuration &&
                       duration <= ReservationRules.MaximumDuration;
            })
            .WithMessage(
                "Reservation duration must be between 30 minutes and 4 hours.")
            .OverridePropertyName(
                nameof(GetReservationAvailabilityQuery.EndsAtUtc));
    }
}

public sealed class GetReservationAvailabilityQueryHandler(
    IReservationDao reservationDao,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetReservationAvailabilityQuery,
        PagedResponse<AvailableReservationTableResponse>>
{
    public Task<PagedResponse<AvailableReservationTableResponse>> Handle(
        GetReservationAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.Access.Permissions.Contains(
                ReservationPermissions.Create,
                StringComparer.Ordinal))
        {
            throw new ForbiddenOperationException();
        }

        return reservationDao.GetAvailabilityAsync(
            request.StartsAtUtc,
            request.EndsAtUtc,
            request.GuestCount,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
