namespace Resturant.Domain.Enums;

public enum SessionRevocationReason
{
    Logout = 1,
    LogoutAll = 2,
    PasswordChanged = 3,
    AccountDeactivated = 4,
    RefreshTokenReuse = 5,
    ConcurrentRefresh = 6,
    AdminRevoked = 7,
    EmailChanged = 8
}