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

namespace Restaurant.Application.Features.Payments.RefundCash;

public sealed record RefundCashPaymentCommand(
    Guid OrderId,
    Guid PaymentId,
    string PaymentVersion,
    string Reason)
    : IRequest<CashPaymentResponse>
{
    public override string ToString() =>
        nameof(RefundCashPaymentCommand);
}

public sealed class RefundCashPaymentCommandValidator
    : AbstractValidator<RefundCashPaymentCommand>
{
    public RefundCashPaymentCommandValidator()
    {
        RuleFor(command => command.OrderId)
            .NotEmpty();

        RuleFor(command => command.PaymentId)
            .NotEmpty();

        RuleFor(command => command.PaymentVersion)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "PaymentVersion must be a valid Base64 SQL Server rowversion.");

        RuleFor(command => command.Reason)
            .NotEmpty()
            .MaximumLength(500);
    }
}

public sealed class RefundCashPaymentCommandHandler(
    ICashPaymentService cashPaymentService)
    : IRequestHandler<
        RefundCashPaymentCommand,
        CashPaymentResponse>
{
    public Task<CashPaymentResponse> Handle(
        RefundCashPaymentCommand request,
        CancellationToken cancellationToken)
    {
        return cashPaymentService.RefundAsync(
            request.OrderId,
            request.PaymentId,
            request.PaymentVersion,
            request.Reason,
            cancellationToken);
    }
}
