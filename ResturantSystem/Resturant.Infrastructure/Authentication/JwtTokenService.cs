using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Common.Exceptions;
using Restaurant.Application.Common.Models;

namespace Resturant.Infrastructure.Authentication;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(
        IOptions<JwtOptions> options,
        TimeProvider timeProvider)
    {
        _options = options.Value;
        _timeProvider = timeProvider;

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.Secret));

        _signingCredentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);
    }

    public AccessTokenResult CreateAccessToken(
        Guid userId,
        Guid sessionId,
        DateTimeOffset sessionExpiresAtUtc)
    {
        if (userId == Guid.Empty ||
            sessionId == Guid.Empty)
        {
            throw new ArgumentException(
                "User and session identifiers are required.");
        }

        var now = _timeProvider.GetUtcNow();

        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(
            now.ToUnixTimeSeconds());

        var configuredExpiry = issuedAt.AddMinutes(
            _options.ExpirationMinutes);

        var expiresAt = configuredExpiry <
                        sessionExpiresAtUtc
            ? configuredExpiry
            : sessionExpiresAtUtc;

        expiresAt = DateTimeOffset.FromUnixTimeSeconds(
            expiresAt.ToUnixTimeSeconds());

        if (expiresAt <= now)
        {
            throw new AuthenticationFailedException();
        }

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                userId.ToString()),

            new Claim(
                "sid",
                sessionId.ToString()),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString("N")),

            new Claim(
                JwtRegisteredClaimNames.Iat,
                issuedAt.ToUnixTimeSeconds()
                    .ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _signingCredentials);

        var encodedToken = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new AccessTokenResult(
            encodedToken,
            expiresAt);
    }
}