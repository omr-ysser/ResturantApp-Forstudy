using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Security;

namespace Restaurant.Application.Features.Permissions.Delete;

public sealed record DeletePermissionCommand(
    Guid Id,
    string Version)
    : IRequest<Unit>;

public sealed class DeletePermissionCommandValidator
    : AbstractValidator<DeletePermissionCommand>
{
    public DeletePermissionCommandValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();

        RuleFor(request => request.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class DeletePermissionCommandHandler(
    IPermissionManagementService permissionService)
    : IRequestHandler<DeletePermissionCommand, Unit>
{
    public async Task<Unit> Handle(
        DeletePermissionCommand request,
        CancellationToken cancellationToken)
    {
        await permissionService.DeleteAsync(
            request.Id,
            request.Version,
            cancellationToken);

        return Unit.Value;
    }
}
