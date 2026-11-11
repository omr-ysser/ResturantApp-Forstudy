using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Abstractions.Authentication;

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(
        Guid userId,
        Guid sessionId,
        DateTimeOffset sessionExpiresAtUtc);
}