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

namespace Restaurant.Application.Features.Users.SetStatus;

public sealed record SetUserStatusCommand(
    Guid UserId,
    string Version,
    bool IsActive)
    : IRequest<UserAccessResponse>;

public sealed class SetUserStatusCommandValidator
    : AbstractValidator<SetUserStatusCommand>
{
    public SetUserStatusCommandValidator()
    {
        RuleFor(request => request.UserId)
            .NotEmpty();

        RuleFor(request => request.Version)
            .Must(RowVersionFormat.IsValid)
            .WithMessage(
                "Version must be a valid Base64 SQL Server rowversion.");
    }
}

public sealed class SetUserStatusCommandHandler(
    IUserAccessService userAccessService)
    : IRequestHandler<
        SetUserStatusCommand,
        UserAccessResponse>
{
    public Task<UserAccessResponse> Handle(
        SetUserStatusCommand request,
        CancellationToken cancellationToken)
    {
        return userAccessService.SetStatusAsync(
            request.UserId,
            request.Version,
            request.IsActive,
            cancellationToken);
    }
}
