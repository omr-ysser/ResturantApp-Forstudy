using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MediatR;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.Authentication.Refresh;

public sealed record RefreshCommand(
    string RefreshToken)
    : IRequest<AuthenticationResult>
{
    public override string ToString()
    {
        return nameof(RefreshCommand);
    }
}

public sealed class RefreshCommandHandler(
    IAuthenticationService authenticationService)
    : IRequestHandler<RefreshCommand, AuthenticationResult>
{
    public Task<AuthenticationResult> Handle(
        RefreshCommand request,
        CancellationToken cancellationToken)
    {
        return authenticationService.RefreshAsync(
            request.RefreshToken,
            cancellationToken);
    }
}
