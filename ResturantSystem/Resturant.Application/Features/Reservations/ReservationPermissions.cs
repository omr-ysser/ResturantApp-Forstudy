namespace Restaurant.Application.Features.Reservations;

public static class ReservationPermissions
{
    public const string Create =
        "Reservations.Create";

    public const string ViewOwn =
        "Reservations.ViewOwn";

    public const string ViewAll =
        "Reservations.ViewAll";

    public const string CancelOwn =
        "Reservations.CancelOwn";

    public const string CancelAll =
        "Reservations.CancelAll";

    public const string Manage =
        "Reservations.Manage";
}