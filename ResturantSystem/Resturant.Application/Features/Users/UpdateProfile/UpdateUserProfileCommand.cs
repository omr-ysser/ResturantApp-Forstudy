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

namespace Restaurant.Application.Features.Users.UpdateProfile;

public sealed record UpdateUserProfileCommand(
    Guid UserId,
    string FullName,
    string Email,
    string Version)
    : IRequest<UserAccessResponse>;

public sealed class UpdateUserProfileCommandValidator
    : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty();

        RuleFor(request => request.FullName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(request => request.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(256)
            .EmailAddress();

        RuleFor(request => request.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class UpdateUserProfileCommandHandler(
    IUserAccountService userAccountService)
    : IRequestHandler<
        UpdateUserProfileCommand,
        UserAccessResponse>
{
    public Task<UserAccessResponse> Handle(
        UpdateUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        return userAccountService.UpdateProfileAsync(
            request.UserId,
            request.FullName,
            request.Email,
            request.Version,
            cancellationToken);
    }
}
