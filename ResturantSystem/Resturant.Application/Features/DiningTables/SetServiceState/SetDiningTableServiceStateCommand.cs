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

namespace Restaurant.Application.Features.DiningTables.SetServiceState;

public sealed record SetDiningTableServiceStateCommand(
    Guid Id,
    bool IsInService,
    string Version)
    : IRequest<DiningTableResponse>;

public sealed class SetDiningTableServiceStateCommandValidator
    : AbstractValidator<SetDiningTableServiceStateCommand>
{
    public SetDiningTableServiceStateCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class SetDiningTableServiceStateCommandHandler(
    IDiningTableService diningTableService)
    : IRequestHandler<
        SetDiningTableServiceStateCommand,
        DiningTableResponse>
{
    public Task<DiningTableResponse> Handle(
        SetDiningTableServiceStateCommand request,
        CancellationToken cancellationToken)
    {
        return diningTableService.SetServiceStateAsync(
            request.Id,
            request.IsInService,
            request.Version,
            cancellationToken);
    }
}
