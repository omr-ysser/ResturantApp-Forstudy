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

namespace Restaurant.Application.Features.Permissions.Update;

public sealed record UpdatePermissionCommand(
    Guid Id,
    string DisplayName,
    string Version)
    : IRequest<PermissionDetailsResponse>;

public sealed class UpdatePermissionCommandValidator
    : AbstractValidator<UpdatePermissionCommand>
{
    public UpdatePermissionCommandValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();

        RuleFor(request => request.DisplayName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class UpdatePermissionCommandHandler(
    IPermissionManagementService permissionService)
    : IRequestHandler<
        UpdatePermissionCommand,
        PermissionDetailsResponse>
{
    public Task<PermissionDetailsResponse> Handle(
        UpdatePermissionCommand request,
        CancellationToken cancellationToken)
    {
        return permissionService.UpdateAsync(
            request.Id,
            request.DisplayName,
            request.Version,
            cancellationToken);
    }
}
