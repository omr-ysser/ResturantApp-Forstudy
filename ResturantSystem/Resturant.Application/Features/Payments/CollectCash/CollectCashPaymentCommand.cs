using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Common.Security;

namespace Restaurant.Application.Features.Payments.CollectCash;

public sealed record CollectCashPaymentCommand(
    Guid OrderId,
    Guid RequestKey,
    string OrderVersion)
    : IRequest<CashCollectionResponse>;

public sealed class CollectCashPaymentCommandValidator
    : AbstractValidator<CollectCashPaymentCommand>
{
    public CollectCashPaymentCommandValidator()
    {
        RuleFor(command => command.OrderId)
            .NotEmpty();

        RuleFor(command => command.RequestKey)
            .NotEmpty();

        RuleFor(command => command.OrderVersion)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "OrderVersion must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class CollectCashPaymentCommandHandler(
    ICashPaymentService cashPaymentService)
    : IRequestHandler<
        CollectCashPaymentCommand,
        CashCollectionResponse>
{
    public Task<CashCollectionResponse> Handle(
        CollectCashPaymentCommand request,
        CancellationToken cancellationToken)
    {
        return cashPaymentService.CollectAsync(
            request.OrderId,
            request.RequestKey,
            request.OrderVersion,
            cancellationToken);
    }
}
