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

namespace Restaurant.Application.Features.Reservations.Get;

public sealed record GetReservationQuery(Guid Id)
    : IRequest<ReservationResponse>;

public sealed class GetReservationQueryValidator
    : AbstractValidator<GetReservationQuery>
{
    public GetReservationQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}

public sealed class GetReservationQueryHandler(
    IReservationDao reservationDao,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetReservationQuery,
        ReservationResponse>
{
    public async Task<ReservationResponse> Handle(
        GetReservationQuery request,
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

        Guid? customerScope = canViewAll
            ? null
            : currentUser.UserId;

        return await reservationDao.GetAsync(
                   request.Id,
                   customerScope,
                   cancellationToken)
               ?? throw new ResourceNotFoundException(
                   "Reservation");
    }
}
