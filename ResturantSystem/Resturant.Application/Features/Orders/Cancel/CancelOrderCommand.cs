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
using Resturant.Domain.Common;

namespace Restaurant.Application.Features.Orders.Cancel;

public sealed record CancelOrderCommand(
    Guid Id,
    string Reason,
    string Version)
    : IRequest<OrderResponse>
{
    public override string ToString() =>
        nameof(CancelOrderCommand);
}

public sealed class CancelOrderCommandValidator
    : AbstractValidator<CancelOrderCommand>
{
    public CancelOrderCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Reason)
            .NotEmpty()
            .MaximumLength(
                OrderRules.MaximumCancellationReasonLength);

        RuleFor(command => command.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class CancelOrderCommandHandler(
    IOrderWorkflowService workflowService)
    : IRequestHandler<
        CancelOrderCommand,
        OrderResponse>
{
    public Task<OrderResponse> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        return workflowService.CancelAsync(
            request.Id,
            request.Reason,
            request.Version,
            cancellationToken);
    }
}
