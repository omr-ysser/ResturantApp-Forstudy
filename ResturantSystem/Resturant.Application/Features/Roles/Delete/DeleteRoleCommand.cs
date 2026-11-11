using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Security;

namespace Restaurant.Application.Features.Roles.Delete;

public sealed record DeleteRoleCommand(
    Guid Id,
    string Version)
    : IRequest<Unit>;

public sealed class DeleteRoleCommandValidator
    : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();

        RuleFor(request => request.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class DeleteRoleCommandHandler(
    IRoleManagementService roleService)
    : IRequestHandler<DeleteRoleCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteRoleCommand request,
        CancellationToken cancellationToken)
    {
        await roleService.DeleteAsync(
            request.Id,
            request.Version,
            cancellationToken);

        return Unit.Value;
    }
}
