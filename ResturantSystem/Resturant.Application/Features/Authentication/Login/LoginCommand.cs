using MediatR;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.Authentication.Login;

public sealed record LoginCommand(
    string Email,
    string Password)
    : IRequest<AuthenticationResult>
{
    public override string ToString()
    {
        return nameof(LoginCommand);
    }
}