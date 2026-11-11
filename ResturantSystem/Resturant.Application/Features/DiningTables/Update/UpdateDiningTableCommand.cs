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

namespace Restaurant.Application.Features.DiningTables.Update;

public sealed record UpdateDiningTableCommand(
    Guid Id,
    int Number,
    int Capacity,
    string Version)
    : IRequest<DiningTableResponse>;

public sealed class UpdateDiningTableCommandValidator
    : AbstractValidator<UpdateDiningTableCommand>
{
    public UpdateDiningTableCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Number)
            .GreaterThan(0);

        RuleFor(command => command.Capacity)
            .InclusiveBetween(1, 100);

        RuleFor(command => command.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class UpdateDiningTableCommandHandler(
    IDiningTableService diningTableService)
    : IRequestHandler<
        UpdateDiningTableCommand,
        DiningTableResponse>
{
    public Task<DiningTableResponse> Handle(
        UpdateDiningTableCommand request,
        CancellationToken cancellationToken)
    {
        return diningTableService.UpdateAsync(
            request.Id,
            request.Number,
            request.Capacity,
            request.Version,
            cancellationToken);
    }
}
