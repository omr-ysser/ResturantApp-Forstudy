using Resturant.Domain.Common;
using Resturant.Domain.Exceptions;

namespace Resturant.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    private RefreshToken()
    {
    }

    public RefreshToken(
        AuthenticationSession session,
        string tokenHash,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException(
                "Token hash is required.");
        }

        if (tokenHash.Length > 128)
        {
            throw new DomainException(
                "Token hash exceeds the maximum length.");
        }

        nowUtc = nowUtc.ToUniversalTime();
        expiresAtUtc = expiresAtUtc.ToUniversalTime();

        if (!session.IsActiveAt(nowUtc))
        {
            throw new DomainException(
                "Cannot create a token for an inactive session.");
        }

        if (expiresAtUtc <= nowUtc)
        {
            throw new DomainException(
                "Token expiry must be in the future.");
        }

        if (expiresAtUtc > session.ExpiresAtUtc)
        {
            throw new DomainException(
                "Token expiry cannot exceed session expiry.");
        }

        UserId = session.UserId;
        SessionId = session.Id;
        Session = session;

        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public Guid? SessionId { get; private set; }

    public AuthenticationSession? Session
    {
        get;
        private set;
    }

    public string TokenHash { get; private set; } =
        string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsActiveAt(DateTimeOffset nowUtc)
    {
        nowUtc = nowUtc.ToUniversalTime();

        return SessionId.HasValue &&
               Session is not null &&
               Session.IsActiveAt(nowUtc) &&
               RevokedAtUtc is null &&
               ExpiresAtUtc > nowUtc;
    }

    public void Revoke(DateTimeOffset nowUtc)
    {
        if (RevokedAtUtc.HasValue)
        {
            return;
        }

        RevokedAtUtc = nowUtc.ToUniversalTime();

        MarkUpdated();
    }

    public void RotateTo(
        RefreshToken replacement,
        DateTimeOffset nowUtc)
    {
        ArgumentNullException.ThrowIfNull(replacement);

        nowUtc = nowUtc.ToUniversalTime();

        var session = Session;

        if (session is null || !IsActiveAt(nowUtc))
        {
            throw new DomainException(
                "Refresh token is inactive.");
        }

        if (replacement.Id == Id ||
            replacement.UserId != UserId ||
            replacement.SessionId != SessionId ||
            replacement.TokenHash == TokenHash ||
            !replacement.IsActiveAt(nowUtc))
        {
            throw new DomainException(
                "Invalid replacement token.");
        }

        session.RecordRefresh(nowUtc);

        ReplacedByTokenId = replacement.Id;

        Revoke(nowUtc);
    }
}