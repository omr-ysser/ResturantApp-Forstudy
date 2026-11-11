using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Resturant.Domain.Common;
using Resturant.Domain.Enums;
using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class AuthenticationSession : BaseEntity
{
    private readonly List<RefreshToken> _refreshTokens = [];

    private AuthenticationSession()
    {
    }

    public AuthenticationSession(
        Guid userId,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset nowUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(
                "User is required.");
        }

        nowUtc = nowUtc.ToUniversalTime();
        expiresAtUtc = expiresAtUtc.ToUniversalTime();

        if (expiresAtUtc <= nowUtc)
        {
            throw new DomainException(
                "Session expiry must be in the future.");
        }

        UserId = userId;
        StartedAtUtc = nowUtc;
        LastUsedAtUtc = nowUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public DateTimeOffset StartedAtUtc { get; private set; }

    public DateTimeOffset LastUsedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public SessionRevocationReason? RevocationReason
    {
        get;
        private set;
    }

    public long RefreshCount { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens =>
        _refreshTokens.AsReadOnly();

    public bool IsActiveAt(DateTimeOffset nowUtc)
    {
        return RevokedAtUtc is null &&
               ExpiresAtUtc > nowUtc.ToUniversalTime();
    }

    public void RecordRefresh(DateTimeOffset nowUtc)
    {
        nowUtc = nowUtc.ToUniversalTime();

        if (!IsActiveAt(nowUtc))
        {
            throw new DomainException(
                "Session is inactive.");
        }

        if (RefreshCount == long.MaxValue)
        {
            throw new DomainException(
                "Session refresh limit reached.");
        }

        RefreshCount++;

        if (nowUtc > LastUsedAtUtc)
        {
            LastUsedAtUtc = nowUtc;
        }

        MarkUpdated();
    }

    public void Revoke(
        SessionRevocationReason reason,
        DateTimeOffset nowUtc)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new DomainException(
                "Invalid session revocation reason.");
        }

        if (RevokedAtUtc.HasValue)
        {
            return;
        }

        RevokedAtUtc = nowUtc.ToUniversalTime();
        RevocationReason = reason;

        MarkUpdated();
    }
}
