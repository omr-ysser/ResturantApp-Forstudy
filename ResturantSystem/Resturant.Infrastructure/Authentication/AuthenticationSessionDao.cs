using Microsoft.EntityFrameworkCore;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Common.Models;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Authentication;

public sealed class AuthenticationSessionDao(
    IAppDbContext dbContext)
    : IAuthenticationSessionDao
{
    public Task<User?> LockUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .FromSqlInterpolated(
                $"""
                SELECT *
                FROM [dbo].[Users] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] = {userId}
                """)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<RefreshTokenReference?> FindTokenReferenceAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens
            .AsNoTracking()
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => new RefreshTokenReference(
                token.UserId,
                token.SessionId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<RefreshToken?> FindTrackedTokenAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens
            .Include(token => token.Session)
            .SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);
    }

    public Task<AuthenticationSession?> FindSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        return dbContext.AuthenticationSessions
            .SingleOrDefaultAsync(
                session =>
                    session.Id == sessionId &&
                    session.UserId == userId,
                cancellationToken);
    }

    public Task<List<AuthenticationSession>>
        GetUnrevokedSessionsAsync(
            Guid userId,
            CancellationToken cancellationToken)
    {
        return dbContext.AuthenticationSessions
            .Where(session =>
                session.UserId == userId &&
                session.RevokedAtUtc == null)
            .OrderBy(session => session.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsSessionActiveAsync(
        Guid userId,
        Guid sessionId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        return dbContext.AuthenticationSessions
            .AsNoTracking()
            .AnyAsync(
                session =>
                    session.Id == sessionId &&
                    session.UserId == userId &&
                    session.RevokedAtUtc == null &&
                    session.ExpiresAtUtc > nowUtc,
                cancellationToken);
    }

    public void AddSession(
        AuthenticationSession session)
    {
        dbContext.AuthenticationSessions.Add(session);
    }

    public void AddRefreshToken(
        RefreshToken token)
    {
        dbContext.RefreshTokens.Add(token);
    }
}