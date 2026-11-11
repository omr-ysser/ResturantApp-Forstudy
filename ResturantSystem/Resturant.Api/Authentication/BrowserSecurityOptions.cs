namespace Resturant.Api.Authentication;

public sealed class BrowserSecurityOptions
{
    public const string SectionName = "BrowserSecurity";

    public string[] AllowedOrigins { get; set; } = [];

    public bool IsValid()
    {
        if (AllowedOrigins.Length == 0)
        {
            return false;
        }

        return AllowedOrigins.All(origin =>
        {
            return Uri.TryCreate(
                       origin,
                       UriKind.Absolute,
                       out var uri) &&
                   uri.Scheme == Uri.UriSchemeHttps &&
                   string.IsNullOrEmpty(uri.UserInfo) &&
                   string.Equals(
                       uri.GetLeftPart(UriPartial.Authority),
                       origin,
                       StringComparison.OrdinalIgnoreCase);
        });
    }
}
