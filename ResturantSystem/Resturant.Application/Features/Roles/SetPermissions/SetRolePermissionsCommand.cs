using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.Roles.SetPermissions;

public sealed record SetRolePermissionsCommand(
    Guid RoleId,
    string Version,
    Guid[] PermissionIds)
    : IRequest<RolePermissionsResponse>;

public sealed class SetRolePermissionsCommandValidator
    : AbstractValidator<SetRolePermissionsCommand>
{
    public SetRolePermissionsCommandValidator()
    {
        RuleFor(request => request.RoleId)
            .NotEmpty();

        RuleFor(request => request.Version)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(24)
            .Must(IsValidRowVersion)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");

        RuleFor(request => request.PermissionIds)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(ids => ids.Length <= 500)
            .WithMessage(
                "A maximum of 500 permissions is allowed.")
            .Must(ids =>
                ids.Distinct().Count() == ids.Length)
            .WithMessage(
                "Permission identifiers must be unique.");

        RuleForEach(request => request.PermissionIds)
            .NotEmpty();
    }

    private static bool IsValidRowVersion(string value)
    {
        try
        {
            return Convert.FromBase64String(value).Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed class SetRolePermissionsCommandHandler(
    IRolePermissionService rolePermissionService)
    : IRequestHandler<
        SetRolePermissionsCommand,
        RolePermissionsResponse>
{
    public Task<RolePermissionsResponse> Handle(
        SetRolePermissionsCommand request,
        CancellationToken cancellationToken)
    {
        return rolePermissionService.SetPermissionsAsync(
            request.RoleId,
            request.Version,
            request.PermissionIds,
            cancellationToken);
    }
}
