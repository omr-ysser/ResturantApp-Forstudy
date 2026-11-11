using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Services;

namespace Restaurant.Application.Features.Authentication.LogoutAll;

public sealed record LogoutAllCommand : IRequest<Unit>;

public sealed class LogoutAllCommandHandler(
    ICurrentUser currentUser,
    IAuthenticationService authenticationService)
    : IRequestHandler<LogoutAllCommand, Unit>
{
    public async Task<Unit> Handle(
        LogoutAllCommand request,
        CancellationToken cancellationToken)
    {
        await authenticationService.LogoutAllAsync(
            currentUser.UserId,
            cancellationToken);

        return Unit.Value;
    }
}
