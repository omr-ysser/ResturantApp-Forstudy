using MediatR;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Common.Models;

namespace Restaurant.Application.Features.Authentication.Me;

public sealed class GetMeQueryHandler(
    ICurrentUser currentUser)
    : IRequestHandler<GetMeQuery, MeResponse>
{
    private static readonly NavigationItem[] Navigation =
    [
        new(
            "users",
            "Users",
            "/users",
            "users",
            ["Users.View"]),

        new(
            "roles",
            "Roles",
            "/roles",
            "shield",
            ["Roles.View"]),

        new(
            "permissions",
            "Permissions",
            "/permissions",
            "key",
            ["Permissions.View"]),

        new(
            "categories",
            "Categories",
            "/categories",
            "folder",
            ["Categories.View"]),

        new(
            "dishes",
            "Dishes",
            "/dishes",
            "utensils",
            ["Dishes.View"]),

        new(
            "menus",
            "Menus",
            "/menus",
            "book-open",
            ["Menus.View"]),

        new(
            "orders",
            "Orders",
            "/orders",
            "receipt",
            ["Orders.ViewAll"]),

        new(
            "my-orders",
            "My orders",
            "/my-orders",
            "shopping-bag",
            ["Orders.ViewOwn"]),

        new(
            "vendor-profile",
            "Vendor profile",
            "/vendor-profile",
            "truck",
            ["Vendors.ViewOwn"])
    ];

    public Task<MeResponse> Handle(
        GetMeQuery request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var access = currentUser.Access;

        var permissions = access.Permissions.ToHashSet(
            StringComparer.Ordinal);

        var navigation = Navigation
            .Where(item =>
                item.RequiredPermissions.All(
                    permissions.Contains))
            .ToArray();

        var response = new MeResponse(
            access.Id,
            access.FullName,
            access.Email,
            access.Roles,
            access.Permissions,
            navigation);

        return Task.FromResult(response);
    }
}