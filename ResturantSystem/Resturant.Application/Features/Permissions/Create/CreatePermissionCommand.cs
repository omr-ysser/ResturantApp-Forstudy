using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.Permissions.Create;

public sealed record CreatePermissionCommand(
    string Key,
    string DisplayName)
    : IRequest<PermissionDetailsResponse>;

public sealed class CreatePermissionCommandValidator
    : AbstractValidator<CreatePermissionCommand>
{
    public CreatePermissionCommandValidator()
    {
        RuleFor(request => request.Key)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(150)
            .Matches(
                @"^[A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z][A-Za-z0-9]*)+$")
            .WithMessage(
                "Key must use dot-separated identifiers, " +
                "for example Reports.Export.");

        RuleFor(request => request.DisplayName)
            .NotEmpty()
            .MaximumLength(200);
    }
}

public sealed class CreatePermissionCommandHandler(
    IPermissionManagementService permissionService)
    : IRequestHandler<
        CreatePermissionCommand,
        PermissionDetailsResponse>
{
    public Task<PermissionDetailsResponse> Handle(
        CreatePermissionCommand request,
        CancellationToken cancellationToken)
    {
        return permissionService.CreateAsync(
            request.Key,
            request.DisplayName,
            cancellationToken);
    }
}
