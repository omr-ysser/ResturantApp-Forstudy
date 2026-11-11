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

namespace Restaurant.Application.Features.Reservations.History;

public sealed record GetReservationHistoryQuery(Guid Id)
    : IRequest<IReadOnlyList<ReservationStatusHistoryResponse>>;

public sealed class GetReservationHistoryQueryValidator
    : AbstractValidator<GetReservationHistoryQuery>
{
    public GetReservationHistoryQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}

public sealed class GetReservationHistoryQueryHandler(
    IReservationDao reservationDao,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetReservationHistoryQuery,
        IReadOnlyList<ReservationStatusHistoryResponse>>
{
    public async Task<IReadOnlyList<ReservationStatusHistoryResponse>> Handle(
        GetReservationHistoryQuery request,
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

        Guid? scope = canViewAll ? null : currentUser.UserId;

        return await reservationDao.GetHistoryAsync(
                   request.Id,
                   scope,
                   cancellationToken)
               ?? throw new ResourceNotFoundException("Reservation");
    }
}
