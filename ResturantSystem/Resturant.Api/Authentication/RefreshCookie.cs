using Restaurant.Application.Common.Models;

namespace Resturant.Api.Authentication;

public sealed class RefreshCookie
{
    public const string Name =
        "__Host-Resturant.Refresh";

    public string Read(HttpRequest request)
    {
        return request.Cookies[Name] ?? string.Empty;
    }

    public void Write(
        HttpResponse response,
        AuthenticationResult result)
    {
        response.Cookies.Append(
            Name,
            result.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                IsEssential = true,
                Expires = result.RefreshExpiresAtUtc
            });
    }

    public void Delete(HttpResponse response)
    {
        response.Cookies.Delete(
            Name,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });
    }
}
