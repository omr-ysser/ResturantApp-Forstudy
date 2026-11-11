using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json.Serialization;

namespace Restaurant.Application.Common.Models;

public sealed class AuthenticationResult
{
    public AuthenticationResult(
        LoginResponse response,
        string refreshToken,
        DateTimeOffset refreshExpiresAtUtc)
    {
        Response = response;
        RefreshToken = refreshToken;
        RefreshExpiresAtUtc = refreshExpiresAtUtc;
    }

    public LoginResponse Response { get; }

    [JsonIgnore]
    public string RefreshToken { get; }

    [JsonIgnore]
    public DateTimeOffset RefreshExpiresAtUtc { get; }

    public override string ToString()
    {
        return nameof(AuthenticationResult);
    }
}
