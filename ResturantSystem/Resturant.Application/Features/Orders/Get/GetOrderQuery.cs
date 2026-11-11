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

namespace Restaurant.Application.Features.Orders.Get;

public sealed record GetOrderQuery(Guid Id)
    : IRequest<OrderResponse>;

public sealed class GetOrderQueryValidator
    : AbstractValidator<GetOrderQuery>
{
    public GetOrderQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}

public sealed class GetOrderQueryHandler(
    IOrderDao orderDao,
    ICurrentUser currentUser)
    : IRequestHandler<GetOrderQuery, OrderResponse>
{
    public async Task<OrderResponse> Handle(
        GetOrderQuery request,
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

        Guid? customerScope = canViewAll
            ? null
            : currentUser.UserId;

        return await orderDao.GetAsync(
                   request.Id,
                   customerScope,
                   cancellationToken)
               ?? throw new ResourceNotFoundException("Order");
    }
}
