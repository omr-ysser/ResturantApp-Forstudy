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

namespace Restaurant.Application.Features.Users.SetRoles;

public sealed record SetUserRolesCommand(
    Guid UserId,
    string Version,
    Guid[] RoleIds)
    : IRequest<UserAccessResponse>;

public sealed class SetUserRolesCommandValidator
    : AbstractValidator<SetUserRolesCommand>
{
    public SetUserRolesCommandValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty();

        RuleFor(request => request.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");

        RuleFor(request => request.RoleIds)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .Must(ids => ids.Length is >= 1 and <= 20)
            .WithMessage(
                "Assign between 1 and 20 roles.")
            .Must(ids =>
                ids.Distinct().Count() == ids.Length)
            .WithMessage(
                "Role identifiers must be unique.");

        RuleForEach(request => request.RoleIds)
            .NotEmpty();
    }
}

public sealed class SetUserRolesCommandHandler(
    IUserAccessService userAccessService)
    : IRequestHandler<
        SetUserRolesCommand,
        UserAccessResponse>
{
    public Task<UserAccessResponse> Handle(
        SetUserRolesCommand request,
        CancellationToken cancellationToken)
    {
        return userAccessService.SetRolesAsync(
            request.UserId,
            request.Version,
            request.RoleIds,
            cancellationToken);
    }
}
