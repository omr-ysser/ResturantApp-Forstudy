namespace Restaurant.Application.Common.Security;

public sealed class SessionOptions
{
    public const string SectionName = "AuthenticationSessions";

    public int AbsoluteLifetimeDays { get; set; } = 30;

    public int RefreshLifetimeDays { get; set; } = 7;
}