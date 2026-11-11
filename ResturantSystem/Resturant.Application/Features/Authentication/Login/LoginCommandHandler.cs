using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.Authentication.Login;

public sealed class LoginCommandHandler(
    IAuthenticationService authenticationService)
    : IRequestHandler<LoginCommand, AuthenticationResult>
{
    public Task<AuthenticationResult> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        return authenticationService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);
    }
}