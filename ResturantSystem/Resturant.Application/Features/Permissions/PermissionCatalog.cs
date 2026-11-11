namespace Restaurant.Application.Features.Permissions;

public static class PermissionCatalog
{
    public const string PermissionsView =
        "Permissions.View";

    public const string CategoriesView =
        "Categories.View";

    public const string CategoriesCreate =
        "Categories.Create";

    public const string CategoriesEdit =
        "Categories.Edit";

    public const string CategoriesDelete =
        "Categories.Delete";

    public static IReadOnlyList<string> All { get; } =
        Array.AsReadOnly(new[]
        {
            "Users.View",
            "Users.Create",
            "Users.Edit",
            "Users.Delete",

            "Roles.View",
            "Roles.Create",
            "Roles.Edit",
            "Roles.Delete",
            "Roles.ManagePermissions",

            "Permissions.View",
            "Permissions.Create",
            "Permissions.Edit",
            "Permissions.Delete",

            "Clients.View",
            "Clients.Create",
            "Clients.Edit",
            "Clients.Delete",

            CategoriesView,
            CategoriesCreate,
            CategoriesEdit,
            CategoriesDelete,

            "Dishes.View",
            "Dishes.Create",
            "Dishes.Edit",
            "Dishes.Delete",

            "Menus.View",
            "Menus.Create",
            "Menus.Edit",
            "Menus.Delete",

            "Orders.ViewOwn",
            "Orders.ViewAll",
            "Orders.Create",
            "Orders.Edit",
            "Orders.CancelOwn",
            "Orders.CancelAll",
            "Orders.ChangeStatus",

            "Reviews.View",
            "Reviews.CreateOwn",
            "Reviews.EditOwn",
            "Reviews.DeleteOwn",
            "Reviews.Moderate",

            "Vendors.ViewOwn"
        });

    public static IReadOnlyList<string> Supervisor { get; } =
        Array.AsReadOnly(new[]
        {
            "Permissions.View",
            "Clients.View",

            CategoriesView,
            CategoriesCreate,
            CategoriesEdit,

            "Dishes.View",
            "Dishes.Create",
            "Dishes.Edit",

            "Menus.View",
            "Menus.Create",
            "Menus.Edit",

            "Orders.ViewAll",
            "Orders.Create",
            "Orders.Edit",
            "Orders.CancelAll",
            "Orders.ChangeStatus",

            "Reviews.View",
            "Reviews.Moderate"
        });

    public static IReadOnlyList<string> Customer { get; } =
        Array.AsReadOnly(new[]
        {
            CategoriesView,

            "Dishes.View",
            "Menus.View",

            "Orders.ViewOwn",
            "Orders.Create",
            "Orders.CancelOwn",

            "Reviews.View",
            "Reviews.CreateOwn",
            "Reviews.EditOwn",
            "Reviews.DeleteOwn"
        });

    public static IReadOnlyList<string> Vendor { get; } =
        Array.AsReadOnly(new[]
        {
            "Vendors.ViewOwn"
        });
}