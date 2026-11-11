using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;
using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Security;

namespace Restaurant.Application.Features.Authentication.ChangePassword;

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword)
    : IRequest<Unit>
{
    public override string ToString()
    {
        return nameof(ChangePasswordCommand);
    }
}

public sealed class ChangePasswordCommandValidator
    : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(request => request.CurrentPassword)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(request => request.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(256)
            .Must(PasswordRules.HasValidLength)
            .WithMessage(
                "New password must contain between 15 and " +
                "128 Unicode code points.")
            .Must((request, newPassword) =>
                !string.Equals(
                    request.CurrentPassword,
                    newPassword,
                    StringComparison.Ordinal))
            .WithMessage(
                "The new password must differ from the current password.");
    }
}

public sealed class ChangePasswordCommandHandler(
    IPasswordChangeService passwordChangeService)
    : IRequestHandler<ChangePasswordCommand, Unit>
{
    public async Task<Unit> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        await passwordChangeService.ChangeAsync(
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        return Unit.Value;
    }
}
