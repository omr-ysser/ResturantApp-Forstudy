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

public sealed class ReviewConfiguration
    : BaseEntityConfiguration<Review>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews", table =>
        {
            table.HasCheckConstraint(
                "CK_Reviews_Rating",
                "[Rating] BETWEEN 1 AND 5");

            table.HasCheckConstraint(
                "CK_Reviews_Deletion",
                """
                (
                    [IsDeleted] = 0
                    AND [DeletedAtUtc] IS NULL
                )
                OR
                (
                    [IsDeleted] = 1
                    AND [DeletedAtUtc] IS NOT NULL
                    AND [Comment] IS NULL
                )
                """);

            table.HasCheckConstraint(
                "CK_Reviews_Moderation",
                """
                (
                    [IsHidden] = 0
                    AND [LastModeratedByUserId] IS NULL
                    AND [LastModeratedAtUtc] IS NULL
                    AND [LastModerationReason] IS NULL
                )
                OR
                (
                    [LastModeratedByUserId] IS NOT NULL
                    AND [LastModeratedAtUtc] IS NOT NULL
                    AND [LastModerationReason] IS NOT NULL
                    AND LEN(LTRIM(RTRIM([LastModerationReason]))) > 0
                )
                """);

            table.HasCheckConstraint(
                "CK_Reviews_DeletionTimestamp",
                """
                [DeletedAtUtc] IS NULL
                OR [DeletedAtUtc] >= [CreatedAtUtc]
                """);

            table.HasCheckConstraint(
                "CK_Reviews_ModerationTimestamp",
                """
                [LastModeratedAtUtc] IS NULL
                OR [LastModeratedAtUtc] >= [CreatedAtUtc]
                """);
        });

        builder.Property(review => review.Rating)
            .IsRequired();

        builder.Property(review => review.Comment)
            .HasMaxLength(
                ReviewRules.MaximumCommentLength);

        builder.Property(review => review.LastModerationReason)
            .HasMaxLength(
                ReviewRules.MaximumModerationReasonLength);

        // The index intentionally includes deleted reviews.
        // Withdrawing a review does not allow another review
        // to be created for the same order.
        builder.HasIndex(review => review.OrderId)
            .IsUnique()
            .HasDatabaseName("UX_Reviews_OrderId");

        builder.HasIndex(review => new
        {
            review.AuthorUserId,
            review.CreatedAtUtc,
            review.Id
        });

        builder.HasIndex(review => new
        {
            review.IsDeleted,
            review.IsHidden,
            review.CreatedAtUtc,
            review.Id
        });

        builder.HasOne(review => review.Order)
            .WithMany()
            .HasForeignKey(review => review.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(review => review.AuthorUser)
            .WithMany()
            .HasForeignKey(review => review.AuthorUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(review => review.LastModeratedByUser)
            .WithMany()
            .HasForeignKey(review =>
                review.LastModeratedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
