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
using Restaurant.Application.Features.Payments;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.Payments.ListForOrder;

public sealed record ListOrderPaymentsQuery(
    Guid OrderId,
    int Page = 1,
    int PageSize = 20,
    PaymentStatus? Status = null)
    : IRequest<PagedResponse<PaymentListItemResponse>>;

public sealed class ListOrderPaymentsQueryValidator
    : AbstractValidator<ListOrderPaymentsQuery>
{
    public ListOrderPaymentsQueryValidator()
    {
        RuleFor(query => query.OrderId)
            .NotEmpty();

        RuleFor(query => query.Page)
            .InclusiveBetween(1, 100_000);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.Status)
            .Must(status =>
                !status.HasValue ||
                Enum.IsDefined(
                    typeof(PaymentStatus),
                    status.Value))
            .WithMessage(
                "Status must be a valid payment status.");
    }
}

public sealed class ListOrderPaymentsQueryHandler(
    IPaymentReadDao paymentReadDao,
    ICurrentUser currentUser)
    : IRequestHandler<
        ListOrderPaymentsQuery,
        PagedResponse<PaymentListItemResponse>>
{
    public async Task<PagedResponse<PaymentListItemResponse>> Handle(
        ListOrderPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        var permissions = currentUser.Access.Permissions;

        var canViewAll = permissions.Contains(
            PaymentPermissions.ViewAll,
            StringComparer.Ordinal);

        var canViewOwn = permissions.Contains(
            PaymentPermissions.ViewOwn,
            StringComparer.Ordinal);

        if (!canViewAll && !canViewOwn)
        {
            throw new ForbiddenOperationException();
        }

        Guid? customerScope = canViewAll
            ? null
            : currentUser.UserId;

        return await paymentReadDao.ListForOrderAsync(
                   request.OrderId,
                   customerScope,
                   request.Page,
                   request.PageSize,
                   request.Status,
                   cancellationToken)
               ?? throw new ResourceNotFoundException("Order");
    }
}
