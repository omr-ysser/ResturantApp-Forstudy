using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Common;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Configurations;

public sealed class ReservationStatusHistoryConfiguration
    : BaseEntityConfiguration<ReservationStatusHistory>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<ReservationStatusHistory> builder)
    {
        builder.ToTable("ReservationStatusHistories", table =>
        {
            table.HasCheckConstraint(
                "CK_ReservationStatusHistories_Transition",
                """
                (
                    [FromStatus] IS NULL
                    AND [ToStatus] = N'Confirmed'
                )
                OR
                (
                    [FromStatus] IS NOT NULL
                    AND
                    (
                        ([FromStatus] = N'Confirmed'
                            AND [ToStatus] = N'Seated')
                        OR
                        ([FromStatus] = N'Seated'
                            AND [ToStatus] = N'Completed')
                        OR
                        ([FromStatus] = N'Confirmed'
                            AND [ToStatus] = N'Cancelled')
                        OR
                        ([FromStatus] = N'Confirmed'
                            AND [ToStatus] = N'NoShow')
                    )
                )
                """);

            table.HasCheckConstraint(
                "CK_ReservationStatusHistories_Reason",
                """
                (
                    [ToStatus] = N'Cancelled'
                    AND [Reason] IS NOT NULL
                    AND LEN(LTRIM(RTRIM([Reason]))) > 0
                )
                OR
                (
                    [ToStatus] <> N'Cancelled'
                    AND [Reason] IS NULL
                )
                """);
        });

        builder.Property(entry => entry.FromStatus)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(entry => entry.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(entry => entry.OccurredAtUtc)
            .IsRequired();

        builder.Property(entry => entry.Reason)
            .HasMaxLength(
                ReservationRules.MaximumReasonLength);

        builder.HasOne(entry => entry.Reservation)
            .WithMany(reservation => reservation.StatusHistory)
            .HasForeignKey(entry => entry.ReservationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(entry => entry.ActorUser)
            .WithMany()
            .HasForeignKey(entry => entry.ActorUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(entry => new
        {
            entry.ReservationId,
            entry.OccurredAtUtc,
            entry.Id
        });
    }
}
