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

namespace Restaurant.Application.Features.Orders.History;

public sealed record GetOrderHistoryQuery(Guid Id)
    : IRequest<IReadOnlyList<OrderStatusHistoryResponse>>;

public sealed class GetOrderHistoryQueryValidator
    : AbstractValidator<GetOrderHistoryQuery>
{
    public GetOrderHistoryQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();
    }
}

public sealed class GetOrderHistoryQueryHandler(
    IOrderWorkflowDao workflowDao,
    ICurrentUser currentUser)
    : IRequestHandler<
        GetOrderHistoryQuery,
        IReadOnlyList<OrderStatusHistoryResponse>>
{
    public async Task<IReadOnlyList<OrderStatusHistoryResponse>> Handle(
        GetOrderHistoryQuery request,
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

        return await workflowDao.GetHistoryAsync(
                   request.Id,
                   customerScope,
                   cancellationToken)
               ?? throw new ResourceNotFoundException("Order");
    }
}
