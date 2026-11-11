using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Resturant.Domain.Entities;

namespace Restaurant.Application.Abstractions.Persistence;

public interface IAppDbContext
{
    DbSet<User> Users { get; }

    DbSet<Role> Roles { get; }

    DbSet<Permission> Permissions { get; }

    DbSet<UserRole> UserRoles { get; }

    DbSet<RolePermission> RolePermissions { get; }

    DbSet<AuthenticationSession> AuthenticationSessions { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Category> Categories { get; }

    DbSet<MenuItem> MenuItems { get; }

    DbSet<DiningTable> DiningTables { get; }

    DbSet<Reservation> Reservations { get; }

    DbSet<Order> Orders { get; }

    DbSet<OrderItem> OrderItems { get; }

    DbSet<Payment> Payments { get; }

    DbSet<Review> Reviews { get; }

    DbSet<CustomerProfile> CustomerProfiles { get; }

    DbSet<DeliveryAddress> DeliveryAddresses { get; }

    Task<IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}