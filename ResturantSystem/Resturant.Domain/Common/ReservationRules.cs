namespace Resturant.Domain.Common;

public static class ReservationRules
{
    public const int MaximumGuestCount = 100;

    public const int MaximumCustomerNameLength = 150;

    public const int MaximumCustomerPhoneLength = 30;

    public const int MaximumReasonLength = 500;

    public static readonly TimeSpan MinimumLeadTime =
        TimeSpan.FromMinutes(15);

    public static readonly TimeSpan MaximumAdvance =
        TimeSpan.FromDays(30);

    public static readonly TimeSpan MinimumDuration =
        TimeSpan.FromMinutes(30);

    public static readonly TimeSpan MaximumDuration =
        TimeSpan.FromHours(4);

    public static readonly TimeSpan EarlySeatingWindow =
        TimeSpan.FromMinutes(15);

    public static readonly TimeSpan NoShowGracePeriod =
        TimeSpan.FromMinutes(15);
}