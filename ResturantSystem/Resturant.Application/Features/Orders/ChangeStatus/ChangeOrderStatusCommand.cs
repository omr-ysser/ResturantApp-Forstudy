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
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.Orders.ChangeStatus;

public sealed record ChangeOrderStatusCommand(
    Guid Id,
    OrderStatus Status,
    string Version)
    : IRequest<OrderResponse>;

public sealed class ChangeOrderStatusCommandValidator
    : AbstractValidator<ChangeOrderStatusCommand>
{
    public ChangeOrderStatusCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");

        RuleFor(command => command.Status)
            .Must(status =>
                status is OrderStatus.Confirmed
                    or OrderStatus.Preparing
                    or OrderStatus.Ready
                    or OrderStatus.Completed)
            .WithMessage(
                "Status must be Confirmed, Preparing, Ready, or Completed. " +
                "Use the cancellation endpoint to cancel an order.");
    }
}

public sealed class ChangeOrderStatusCommandHandler(
    IOrderWorkflowService workflowService)
    : IRequestHandler<
        ChangeOrderStatusCommand,
        OrderResponse>
{
    public Task<OrderResponse> Handle(
        ChangeOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        return workflowService.ChangeStatusAsync(
            request.Id,
            request.Status,
            request.Version,
            cancellationToken);
    }
}
