using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Common;

namespace Restaurant.Application.Features.Reservations.Create;

public sealed record CreateReservationCommand(
    Guid RequestKey,
    CreateReservationRequest Request)
    : IRequest<ReservationCreationResult>
{
    public override string ToString() =>
        nameof(CreateReservationCommand);
}

public sealed class CreateReservationCommandValidator
    : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(command => command.RequestKey)
            .NotEmpty();

        RuleFor(command => command.Request)
            .NotNull()
            .SetValidator(
                new CreateReservationRequestValidator());
    }
}

public sealed class CreateReservationRequestValidator
    : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationRequestValidator()
    {
        RuleFor(request => request.DiningTableId)
            .NotEmpty();

        RuleFor(request => request.CustomerName)
            .NotEmpty()
            .MaximumLength(
                ReservationRules.MaximumCustomerNameLength);

        RuleFor(request => request.CustomerPhone)
            .NotEmpty()
            .MaximumLength(
                ReservationRules.MaximumCustomerPhoneLength);

        RuleFor(request => request.GuestCount)
            .InclusiveBetween(
                1,
                ReservationRules.MaximumGuestCount);

        RuleFor(request => request.StartsAtUtc)
            .NotEmpty();

        RuleFor(request => request.EndsAtUtc)
            .NotEmpty();

        RuleFor(request => request)
            .Must(request =>
            {
                var duration =
                    request.EndsAtUtc - request.StartsAtUtc;

                return duration >= ReservationRules.MinimumDuration &&
                       duration <= ReservationRules.MaximumDuration;
            })
            .WithMessage(
                "Reservation duration must be between 30 minutes and 4 hours.")
            .OverridePropertyName(
                nameof(CreateReservationRequest.EndsAtUtc));
    }
}

public sealed class CreateReservationCommandHandler(
    IReservationService reservationService)
    : IRequestHandler<
        CreateReservationCommand,
        ReservationCreationResult>
{
    public Task<ReservationCreationResult> Handle(
        CreateReservationCommand request,
        CancellationToken cancellationToken)
    {
        return reservationService.CreateAsync(
            request.RequestKey,
            request.Request,
            cancellationToken);
    }
}
