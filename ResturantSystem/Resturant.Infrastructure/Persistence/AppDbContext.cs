using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Restaurant.Application.Abstractions.Persistence;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence;

public sealed class AppDbContext
    : DbContext, IAppDbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users =>
        Set<User>();

    public DbSet<Role> Roles =>
        Set<Role>();

    public DbSet<Permission> Permissions =>
        Set<Permission>();

    public DbSet<UserRole> UserRoles =>
        Set<UserRole>();

    public DbSet<RolePermission> RolePermissions =>
        Set<RolePermission>();

    public DbSet<AuthenticationSession> AuthenticationSessions =>
        Set<AuthenticationSession>();

    public DbSet<RefreshToken> RefreshTokens =>
        Set<RefreshToken>();

    public DbSet<Category> Categories =>
        Set<Category>();

    public DbSet<MenuItem> MenuItems =>
        Set<MenuItem>();

    public DbSet<DiningTable> DiningTables =>
        Set<DiningTable>();

    public DbSet<Reservation> Reservations =>
        Set<Reservation>();

    public DbSet<Order> Orders =>
        Set<Order>();

    public DbSet<OrderItem> OrderItems =>
        Set<OrderItem>();

    public DbSet<Payment> Payments =>
        Set<Payment>();

    public DbSet<Review> Reviews =>
        Set<Review>();

    public DbSet<CustomerProfile> CustomerProfiles =>
        Set<CustomerProfile>();

    public DbSet<DeliveryAddress> DeliveryAddresses =>
        Set<DeliveryAddress>();

    public Task<IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(
            cancellationToken);
    }

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }
}