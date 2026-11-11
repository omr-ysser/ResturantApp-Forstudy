using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Common;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration
    : BaseEntityConfiguration<Reservation>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations", table =>
        {
            table.HasCheckConstraint(
                "CK_Reservations_GuestCount",
                "[GuestCount] > 0");

            table.HasCheckConstraint(
                "CK_Reservations_TimeRange",
                "[EndsAtUtc] > [StartsAtUtc]");

            table.HasCheckConstraint(
                "CK_Reservations_RequestMetadata",
                """
                (
                    [RequestKey] IS NULL
                    AND [RequestHash] IS NULL
                )
                OR
                (
                    [RequestKey] IS NOT NULL
                    AND [RequestHash] IS NOT NULL
                    AND [CreatedByUserId] IS NOT NULL
                )
                """);
        });

        builder.Property(reservation => reservation.CustomerName)
            .HasMaxLength(
                ReservationRules.MaximumCustomerNameLength)
            .IsRequired();

        builder.Property(reservation => reservation.CustomerPhone)
            .HasMaxLength(
                ReservationRules.MaximumCustomerPhoneLength)
            .IsRequired();

        builder.Property(reservation => reservation.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(reservation => reservation.RequestHash)
            .HasColumnType("char(64)")
            .IsUnicode(false);

        builder.HasOne(reservation => reservation.DiningTable)
            .WithMany()
            .HasForeignKey(reservation =>
                reservation.DiningTableId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(reservation => reservation.CreatedByUser)
            .WithMany()
            .HasForeignKey(reservation =>
                reservation.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(reservation => reservation.CustomerUser)
            .WithMany()
            .HasForeignKey(reservation =>
                reservation.CustomerUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Navigation(reservation => reservation.StatusHistory)
            .HasField("_statusHistory")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(reservation => new
        {
            reservation.DiningTableId,
            reservation.Status,
            reservation.StartsAtUtc,
            reservation.EndsAtUtc
        })
            .HasDatabaseName(
                "IX_Reservations_Table_Status_TimeRange");

        builder.HasIndex(reservation => new
        {
            reservation.CustomerUserId,
            reservation.StartsAtUtc,
            reservation.Id
        });

        builder.HasIndex(reservation => new
        {
            reservation.CreatedByUserId,
            reservation.RequestKey
        })
            .IsUnique()
            .HasFilter(
                "[CreatedByUserId] IS NOT NULL " +
                "AND [RequestKey] IS NOT NULL")
            .HasDatabaseName(
                "UX_Reservations_Creator_RequestKey");
    }
}