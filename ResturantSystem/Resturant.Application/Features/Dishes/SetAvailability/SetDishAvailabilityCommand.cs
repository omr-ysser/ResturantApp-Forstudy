using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Features.Dishes;

namespace Restaurant.Application.Features.Dishes.SetAvailability;

public sealed record SetDishAvailabilityCommand(
    Guid Id,
    bool IsAvailable,
    string Version)
    : IRequest<DishResponse>;

public sealed class SetDishAvailabilityCommandValidator
    : AbstractValidator<SetDishAvailabilityCommand>
{
    public SetDishAvailabilityCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Version)
            .Must(DishInputRules.IsValidVersion)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class SetDishAvailabilityCommandHandler(
    IDishService dishService)
    : IRequestHandler<
        SetDishAvailabilityCommand,
        DishResponse>
{
    public Task<DishResponse> Handle(
        SetDishAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        return dishService.SetAvailabilityAsync(
            request.Id,
            request.IsAvailable,
            request.Version,
            cancellationToken);
    }
}
