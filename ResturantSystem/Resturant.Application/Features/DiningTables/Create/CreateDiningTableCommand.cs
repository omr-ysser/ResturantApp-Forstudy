using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.DiningTables.Create;

public sealed record CreateDiningTableCommand(
    int Number,
    int Capacity)
    : IRequest<DiningTableResponse>;

public sealed class CreateDiningTableCommandValidator
    : AbstractValidator<CreateDiningTableCommand>
{
    public CreateDiningTableCommandValidator()
    {
        RuleFor(command => command.Number)
            .GreaterThan(0);

        RuleFor(command => command.Capacity)
            .InclusiveBetween(1, 100);
    }
}

public sealed class CreateDiningTableCommandHandler(
    IDiningTableService diningTableService)
    : IRequestHandler<
        CreateDiningTableCommand,
        DiningTableResponse>
{
    public Task<DiningTableResponse> Handle(
        CreateDiningTableCommand request,
        CancellationToken cancellationToken)
    {
        return diningTableService.CreateAsync(
            request.Number,
            request.Capacity,
            cancellationToken);
    }
}
