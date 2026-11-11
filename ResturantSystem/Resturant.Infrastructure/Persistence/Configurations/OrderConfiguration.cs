using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Common;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration
    : BaseEntityConfiguration<Order>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint(
                "CK_Orders_Reservation_DineIn",
                """
                [ReservationId] IS NULL
                OR
                (
                    [Type] = N'DineIn'
                    AND [DiningTableId] IS NOT NULL
                )
                """);

            table.HasCheckConstraint(
                "CK_Orders_WalkInPlan",
                """
                (
                    [DiningStartedAtUtc] IS NULL
                    AND [ExpectedDepartureAtUtc] IS NULL
                )
                OR
                (
                    [DiningStartedAtUtc] IS NOT NULL
                    AND [ExpectedDepartureAtUtc] IS NOT NULL
                    AND [ExpectedDepartureAtUtc] > [DiningStartedAtUtc]
                    AND [Type] = N'DineIn'
                    AND [DiningTableId] IS NOT NULL
                    AND [ReservationId] IS NULL
                )
                """);
        });

        builder.Property(order => order.OrderNumber)
            .HasMaxLength(OrderRules.MaximumOrderNumberLength)
            .IsRequired();

        builder.HasIndex(order => order.OrderNumber)
            .IsUnique();

        builder.Property(order => order.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(order => order.CustomerName)
            .HasMaxLength(
                OrderRules.MaximumCustomerNameLength);

        builder.Property(order => order.CustomerPhone)
            .HasMaxLength(
                OrderRules.MaximumCustomerPhoneLength);

        builder.Property(order => order.DeliveryAddress)
            .HasMaxLength(
                OrderRules.MaximumDeliveryAddressLength);

        builder.Property(order => order.Notes)
            .HasMaxLength(
                OrderRules.MaximumNotesLength);

        builder.Property(order => order.CancellationReason)
            .HasMaxLength(
                OrderRules.MaximumCancellationReasonLength);

        builder.Ignore(order => order.TotalAmount);

        builder.HasOne(order => order.CreatedByUser)
            .WithMany()
            .HasForeignKey(order => order.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(order => order.CustomerUser)
            .WithMany()
            .HasForeignKey(order => order.CustomerUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(order => order.DiningTable)
            .WithMany()
            .HasForeignKey(order => order.DiningTableId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(order => order.Reservation)
            .WithMany()
            .HasForeignKey(order => order.ReservationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(order => order.Items)
            .WithOne(item => item.Order)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(order => order.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(order => order.StatusHistory)
            .HasField("_statusHistory")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(order => new
        {
            order.Status,
            order.CreatedAtUtc
        });

        builder.HasIndex(order => new
        {
            order.CustomerUserId,
            order.CreatedAtUtc,
            order.Id
        });

        builder.HasIndex(order => new
        {
            order.DiningTableId,
            order.Status,
            order.ExpectedDepartureAtUtc
        });

        builder.HasIndex(order => order.ReservationId)
            .IsUnique()
            .HasFilter(
                "[ReservationId] IS NOT NULL " +
                "AND [Status] <> N'Completed' " +
                "AND [Status] <> N'Cancelled'")
            .HasDatabaseName(
                "UX_Orders_Reservation_Active");
    }
}