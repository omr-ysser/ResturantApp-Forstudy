using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Restaurant.Application.Abstractions.Authentication;
using Restaurant.Application.Abstractions.Persistence;
using Restaurant.Application.Features.Permissions;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Seeding;
//{
//  "email": "admin@example.com",
//  "password": "LocalDemo!2026-Only"
//}
//{
//  "email": "supervisor@example.com",
//  "password": "LocalDemo!2026-Only"
//}
public sealed class DevelopmentSeeder(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher,
    IConfiguration configuration)
{
    public async Task SeedAsync(
        CancellationToken cancellationToken)
    {
        var permissionMap = await dbContext.Permissions
            .ToDictionaryAsync(
                permission => permission.NormalizedKey,
                cancellationToken);

        foreach (var key in PermissionCatalog.All)
        {
            var normalizedKey = key.ToUpperInvariant();

            if (permissionMap.ContainsKey(normalizedKey))
            {
                continue;
            }

            var permission = new Permission(
                key,
                key);

            dbContext.Permissions.Add(permission);

            permissionMap.Add(
                normalizedKey,
                permission);
        }

        var roleDefinitions =
            new (
                string Name,
                IReadOnlyList<string> Permissions)[]
            {
                ("Admin", PermissionCatalog.All),
                ("Supervisor", PermissionCatalog.Supervisor),
                ("Vendor", PermissionCatalog.Vendor),
                ("Customer", PermissionCatalog.Customer)
            };

        var roleMap = await dbContext.Roles
            .ToDictionaryAsync(
                role => role.NormalizedName,
                cancellationToken);

        foreach (var definition in roleDefinitions)
        {
            var normalizedName =
                definition.Name.ToUpperInvariant();

            if (roleMap.ContainsKey(normalizedName))
            {
                // Preserve existing administrative changes.
                continue;
            }

            var role = new Role(definition.Name);

            foreach (var key in definition.Permissions)
            {
                var permission = permissionMap[
                    key.ToUpperInvariant()];

                role.GrantPermission(permission.Id);
            }

            dbContext.Roles.Add(role);

            roleMap.Add(
                normalizedName,
                role);
        }

        var accounts = new[]
        {
            new
            {
                Role = "Admin",
                Name = "Admin Demo",
                Email = "admin@example.com"
            },
            new
            {
                Role = "Supervisor",
                Name = "Supervisor Demo",
                Email = "supervisor@example.com"
            },
            new
            {
                Role = "Vendor",
                Name = "Vendor Demo",
                Email = "vendor@example.com"
            },
            new
            {
                Role = "Customer",
                Name = "Customer Demo",
                Email = "customer@example.com"
            }
        };

        var password =
            configuration["Seed:DemoPassword"];

        foreach (var account in accounts)
        {
            var normalizedEmail =
                account.Email.ToUpperInvariant();

            var exists = await dbContext.Users.AnyAsync(
                user =>
                    user.NormalizedEmail == normalizedEmail,
                cancellationToken);

            if (exists)
            {
                // Do not reset existing accounts.
                continue;
            }

            if (string.IsNullOrWhiteSpace(password) ||
                password.Length < 12)
            {
                throw new InvalidOperationException(
                    "Seed:DemoPassword must contain at least " +
                    "12 characters when creating demo accounts.");
            }

            var user = new User(
                account.Name,
                account.Email,
                passwordHasher.Hash(password));

            var role = roleMap[
                account.Role.ToUpperInvariant()];

            user.AssignRole(role.Id);

            dbContext.Users.Add(user);
        }

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
