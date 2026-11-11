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
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.Reservations.ChangeState;

public sealed record ChangeReservationStateCommand(
    Guid Id,
    ReservationStatus TargetStatus,
    string Version,
    string? Reason = null)
    : IRequest<ReservationResponse>
{
    public override string ToString() =>
        nameof(ChangeReservationStateCommand);
}

public sealed class ChangeReservationStateCommandValidator
    : AbstractValidator<ChangeReservationStateCommand>
{
    public ChangeReservationStateCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");

        RuleFor(command => command.TargetStatus)
            .Must(status =>
                status is ReservationStatus.Seated
                    or ReservationStatus.Completed
                    or ReservationStatus.Cancelled
                    or ReservationStatus.NoShow)
            .WithMessage(
                "Unsupported reservation action.");

        RuleFor(command => command.Reason)
            .MaximumLength(
                ReservationRules.MaximumReasonLength);

        When(
            command =>
                command.TargetStatus == ReservationStatus.Cancelled,
            () =>
            {
                RuleFor(command => command.Reason)
                    .NotEmpty();
            });

        When(
            command =>
                command.TargetStatus != ReservationStatus.Cancelled,
            () =>
            {
                RuleFor(command => command.Reason)
                    .Null();
            });
    }
}

public sealed class ChangeReservationStateCommandHandler(
    IReservationService reservationService)
    : IRequestHandler<
        ChangeReservationStateCommand,
        ReservationResponse>
{
    public Task<ReservationResponse> Handle(
        ChangeReservationStateCommand request,
        CancellationToken cancellationToken)
    {
        return reservationService.ChangeStateAsync(
            request.Id,
            request.TargetStatus,
            request.Version,
            request.Reason,
            cancellationToken);
    }
}
