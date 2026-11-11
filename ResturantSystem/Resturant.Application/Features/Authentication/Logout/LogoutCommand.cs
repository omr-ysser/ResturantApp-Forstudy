using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Services;

namespace Restaurant.Application.Features.Authentication.Logout;

public sealed record LogoutCommand : IRequest<Unit>;

public sealed class LogoutCommandHandler(
    ICurrentUser currentUser,
    IAuthenticationService authenticationService)
    : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        await authenticationService.LogoutAsync(
            currentUser.UserId,
            currentUser.SessionId,
            cancellationToken);

        return Unit.Value;
    }
}
