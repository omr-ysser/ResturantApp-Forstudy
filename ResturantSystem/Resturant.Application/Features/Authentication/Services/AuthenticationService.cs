using Microsoft.EntityFrameworkCore.Storage;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Dao;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Abstractions.Services;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;
using Restaurant.Application.Common.Security;
using Resturant.Domain.Entities;
using Resturant.Domain.Enums;

namespace Restaurant.Application.Features.Authentication.Services;

public sealed class AuthenticationService(
    IAuthenticationDao authenticationDao,
    IAuthenticationSessionDao sessionDao,
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IRefreshTokenGenerator refreshTokenGenerator,
    ITokenService tokenService,
    DummyPasswordHash dummyPasswordHash,
    SessionOptions sessionOptions,
    TimeProvider timeProvider)
    : IAuthenticationService
{
    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail =
            email.Trim().ToUpperInvariant();

        var loginUser =
            await authenticationDao.FindLoginUserAsync(
                normalizedEmail,
                cancellationToken);

        var hash = loginUser?.PasswordHash
                   ?? dummyPasswordHash.Value;

        var passwordIsValid = passwordHasher.Verify(
            password,
            hash);

        if (loginUser is null ||
            !loginUser.IsActive ||
            !passwordIsValid)
        {
            throw new AuthenticationFailedException();
        }

        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var user = await sessionDao.LockUserAsync(
            loginUser.Id,
            cancellationToken);

        if (user is null ||
            !user.IsActive ||
            user.PasswordHash != loginUser.PasswordHash)
        {
            throw new AuthenticationFailedException();
        }

        var access = await authenticationDao.GetAccessAsync(
            user.Id,
            cancellationToken);

        if (access is null)
        {
            throw new AuthenticationFailedException();
        }

        var now = timeProvider.GetUtcNow();

        var session = new AuthenticationSession(
            user.Id,
            now.AddDays(
                sessionOptions.AbsoluteLifetimeDays),
            now);

        var rawRefreshToken =
            refreshTokenGenerator.Generate();

        var refreshToken = CreateRefreshToken(
            session,
            rawRefreshToken,
            now);

        sessionDao.AddSession(session);
        sessionDao.AddRefreshToken(refreshToken);

        var result = CreateResult(
            access,
            session,
            refreshToken,
            rawRefreshToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return result;
    }

    public async Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) ||
            refreshToken.Length != 86 ||
            refreshToken.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character != '-' &&
                character != '_'))
        {
            throw new AuthenticationFailedException();
        }

        var tokenHash =
            refreshTokenGenerator.Hash(refreshToken);

        var reference =
            await sessionDao.FindTokenReferenceAsync(
                tokenHash,
                cancellationToken);

        if (reference is null ||
            !reference.SessionId.HasValue)
        {
            throw new AuthenticationFailedException();
        }

        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var user = await sessionDao.LockUserAsync(
            reference.UserId,
            cancellationToken);

        var token = await sessionDao.FindTrackedTokenAsync(
            tokenHash,
            cancellationToken);

        if (user is null ||
            token is null ||
            token.UserId != user.Id ||
            token.Session is null)
        {
            throw new AuthenticationFailedException();
        }

        var session = token.Session;
        var now = timeProvider.GetUtcNow();

        if (!user.IsActive)
        {
            await RevokeAndRejectAsync(
                session,
                SessionRevocationReason.AccountDeactivated,
                now,
                transaction,
                cancellationToken);
        }

        if (!session.IsActiveAt(now))
        {
            throw new AuthenticationFailedException();
        }

        if (token.RevokedAtUtc.HasValue &&
            token.ReplacedByTokenId.HasValue)
        {
            await RevokeAndRejectAsync(
                session,
                SessionRevocationReason.RefreshTokenReuse,
                now,
                transaction,
                cancellationToken);
        }

        if (!token.IsActiveAt(now))
        {
            throw new AuthenticationFailedException();
        }

        var access = await authenticationDao.GetAccessAsync(
            user.Id,
            cancellationToken);

        if (access is null)
        {
            throw new AuthenticationFailedException();
        }

        var newRawToken =
            refreshTokenGenerator.Generate();

        var replacement = CreateRefreshToken(
            session,
            newRawToken,
            now);

        token.RotateTo(replacement, now);

        sessionDao.AddRefreshToken(replacement);

        var result = CreateResult(
            access,
            session,
            replacement,
            newRawToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return result;
    }

    public async Task LogoutAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var user = await sessionDao.LockUserAsync(
            userId,
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new AuthenticationFailedException();
        }

        var session = await sessionDao.FindSessionAsync(
            userId,
            sessionId,
            cancellationToken);

        if (session is null)
        {
            throw new AuthenticationFailedException();
        }

        session.Revoke(
            SessionRevocationReason.Logout,
            timeProvider.GetUtcNow());

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    public async Task LogoutAllAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await dbContext.BeginTransactionAsync(
                cancellationToken);

        var user = await sessionDao.LockUserAsync(
            userId,
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new AuthenticationFailedException();
        }

        var sessions =
            await sessionDao.GetUnrevokedSessionsAsync(
                userId,
                cancellationToken);

        var now = timeProvider.GetUtcNow();

        foreach (var session in sessions)
        {
            session.Revoke(
                SessionRevocationReason.LogoutAll,
                now);
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }

    private RefreshToken CreateRefreshToken(
        AuthenticationSession session,
        string rawToken,
        DateTimeOffset now)
    {
        var expiresAt = now.AddDays(
            sessionOptions.RefreshLifetimeDays);

        if (expiresAt > session.ExpiresAtUtc)
        {
            expiresAt = session.ExpiresAtUtc;
        }

        return new RefreshToken(
            session,
            refreshTokenGenerator.Hash(rawToken),
            expiresAt,
            now);
    }

    private AuthenticationResult CreateResult(
        AccessProfile access,
        AuthenticationSession session,
        RefreshToken refreshToken,
        string rawRefreshToken)
    {
        var accessToken = tokenService.CreateAccessToken(
            access.Id,
            session.Id,
            session.ExpiresAtUtc);

        var response = new LoginResponse(
            accessToken.AccessToken,
            accessToken.ExpiresAtUtc,
            accessToken.TokenType,
            access);

        return new AuthenticationResult(
            response,
            rawRefreshToken,
            refreshToken.ExpiresAtUtc);
    }

    private async Task RevokeAndRejectAsync(
        AuthenticationSession session,
        SessionRevocationReason reason,
        DateTimeOffset now,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        session.Revoke(reason, now);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        throw new AuthenticationFailedException();
    }
}