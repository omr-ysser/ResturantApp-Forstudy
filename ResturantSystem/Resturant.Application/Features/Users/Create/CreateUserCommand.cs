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

namespace Restaurant.Application.Features.Users.Create;

public sealed record CreateUserCommand(
    string FullName,
    string Email,
    string Password,
    Guid[] RoleIds)
    : IRequest<UserAccessResponse>
{
    public override string ToString()
    {
        return nameof(CreateUserCommand);
    }
}

public sealed class CreateUserCommandValidator
    : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(request => request.FullName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(256)
            .EmailAddress();

        RuleFor(request => request.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(256)
            .Must(PasswordRules.HasValidLength)
            .WithMessage(
                "Password must contain between 15 and " +
                "128 Unicode code points.");

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

public sealed class CreateUserCommandHandler(
    IUserAccountService userAccountService)
    : IRequestHandler<
        CreateUserCommand,
        UserAccessResponse>
{
    public Task<UserAccessResponse> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        return userAccountService.CreateAsync(
            request.FullName,
            request.Email,
            request.Password,
            request.RoleIds,
            cancellationToken);
    }
}