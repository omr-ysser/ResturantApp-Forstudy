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

namespace Restaurant.Application.Features.Roles.Update;

public sealed record UpdateRoleCommand(
    Guid Id,
    string Name,
    string Version)
    : IRequest<RolePermissionsResponse>;

public sealed class UpdateRoleCommandValidator
    : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(request => request.Id)
            .NotEmpty();

        RuleFor(request => request.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(100)
            .Must(name => name.Trim().Length >= 2)
            .WithMessage(
                "Role name must contain at least two characters.")
            .Matches(@"^[A-Za-z][A-Za-z0-9 _-]*$")
            .WithMessage(
                "Role name must start with a letter and contain " +
                "only letters, digits, spaces, underscores or hyphens.");

        RuleFor(request => request.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class UpdateRoleCommandHandler(
    IRoleManagementService roleService)
    : IRequestHandler<
        UpdateRoleCommand,
        RolePermissionsResponse>
{
    public Task<RolePermissionsResponse> Handle(
        UpdateRoleCommand request,
        CancellationToken cancellationToken)
    {
        return roleService.UpdateAsync(
            request.Id,
            request.Name,
            request.Version,
            cancellationToken);
    }
}
