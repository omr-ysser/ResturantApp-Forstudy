using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.Roles.Create;

public sealed record CreateRoleCommand(
    string Name)
    : IRequest<RolePermissionsResponse>;

public sealed class CreateRoleCommandValidator
    : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
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
    }
}

public sealed class CreateRoleCommandHandler(
    IRoleManagementService roleService)
    : IRequestHandler<
        CreateRoleCommand,
        RolePermissionsResponse>
{
    public Task<RolePermissionsResponse> Handle(
        CreateRoleCommand request,
        CancellationToken cancellationToken)
    {
        return roleService.CreateAsync(
            request.Name,
            cancellationToken);
    }
}
