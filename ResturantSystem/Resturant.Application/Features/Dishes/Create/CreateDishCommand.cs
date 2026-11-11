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

namespace Restaurant.Application.Features.Dishes.Create;

public sealed record CreateDishCommand(
    Guid CategoryId,
    string Name,
    decimal Price,
    string? Description)
    : IRequest<DishResponse>;

public sealed class CreateDishCommandValidator
    : AbstractValidator<CreateDishCommand>
{
    public CreateDishCommandValidator()
    {
        RuleFor(command => command.CategoryId)
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
    }
}

public sealed class CreateDishCommandHandler(
    IDishService dishService)
    : IRequestHandler<CreateDishCommand, DishResponse>
{
    public Task<DishResponse> Handle(
        CreateDishCommand request,
        CancellationToken cancellationToken)
    {
        return dishService.CreateAsync(
            request.CategoryId,
            request.Name,
            request.Price,
            request.Description,
            cancellationToken);
    }
}
