using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Restaurant.Application.Common.Models;

using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Dao;

public interface IAuthenticationSessionDao
{
    Task<User?> LockUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<RefreshTokenReference?> FindTokenReferenceAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    Task<RefreshToken?> FindTrackedTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken);

    Task<AuthenticationSession?> FindSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<List<AuthenticationSession>> GetUnrevokedSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<bool> IsSessionActiveAsync(
        Guid userId,
        Guid sessionId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken);

    void AddSession(AuthenticationSession session);

    void AddRefreshToken(RefreshToken token);
}
