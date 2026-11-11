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

public sealed class OrderStatusHistoryConfiguration
    : BaseEntityConfiguration<OrderStatusHistory>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("OrderStatusHistories", table =>
        {
            table.HasCheckConstraint(
                "CK_OrderStatusHistories_Transition",
                """
                (
                    [FromStatus] IS NULL
                    AND [ToStatus] = N'Pending'
                )
                OR
                (
                    [FromStatus] IS NOT NULL
                    AND
                    (
                        ([FromStatus] = N'Pending'
                            AND [ToStatus] = N'Confirmed')
                        OR
                        ([FromStatus] = N'Confirmed'
                            AND [ToStatus] = N'Preparing')
                        OR
                        ([FromStatus] = N'Preparing'
                            AND [ToStatus] = N'Ready')
                        OR
                        ([FromStatus] = N'Ready'
                            AND [ToStatus] = N'Completed')
                        OR
                        ([FromStatus] IN (N'Pending', N'Confirmed')
                            AND [ToStatus] = N'Cancelled')
                    )
                )
                """);

            table.HasCheckConstraint(
                "CK_OrderStatusHistories_Reason",
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
                OrderRules.MaximumCancellationReasonLength);

        builder.HasOne(entry => entry.Order)
            .WithMany(order => order.StatusHistory)
            .HasForeignKey(entry => entry.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(entry => entry.ActorUser)
            .WithMany()
            .HasForeignKey(entry => entry.ActorUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(entry => new
        {
            entry.OrderId,
            entry.OccurredAtUtc,
            entry.Id
        });
    }
}
