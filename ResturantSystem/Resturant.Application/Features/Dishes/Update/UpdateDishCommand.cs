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

namespace Restaurant.Application.Features.Dishes.Update;

public sealed record UpdateDishCommand(
    Guid Id,
    string Name,
    decimal Price,
    string? Description,
    string Version)
    : IRequest<DishResponse>;

public sealed class UpdateDishCommandValidator
    : AbstractValidator<UpdateDishCommand>
{
    public UpdateDishCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(command => command.Description)
            .MaximumLength(1000);

        RuleFor(command => command.Price)
            .Must(DishInputRules.IsValidPrice)
            .WithMessage(
                "Price must be positive, fit decimal(18,2), " +
                "and contain at most two decimal places.");

        RuleFor(command => command.Version)
            .Must(DishInputRules.IsValidVersion)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class UpdateDishCommandHandler(
    IDishService dishService)
    : IRequestHandler<UpdateDishCommand, DishResponse>
{
    public Task<DishResponse> Handle(
        UpdateDishCommand request,
        CancellationToken cancellationToken)
    {
        return dishService.UpdateAsync(
            request.Id,
            request.Name,
            request.Price,
            request.Description,
            request.Version,
            cancellationToken);
    }
}
