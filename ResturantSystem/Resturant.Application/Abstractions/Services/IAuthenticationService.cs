using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;


namespace Restaurant.Application.Abstractions.Services;

public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task LogoutAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken);

    Task LogoutAllAsync(
        Guid userId,
        CancellationToken cancellationToken);
}
