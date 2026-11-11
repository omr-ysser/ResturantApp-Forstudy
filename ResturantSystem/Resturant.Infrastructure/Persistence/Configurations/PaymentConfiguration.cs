using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration
    : BaseEntityConfiguration<Payment>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", table =>
        {
            table.HasCheckConstraint(
                "CK_Payments_Amount",
                "[Amount] > 0");

            table.HasCheckConstraint(
                "CK_Payments_CashCollectionAudit",
                """
                (
                    [CollectedByUserId] IS NULL
                    AND [CollectionRequestKey] IS NULL
                )
                OR
                (
                    [CollectedByUserId] IS NOT NULL
                    AND [CollectionRequestKey] IS NOT NULL
                    AND [Method] = N'Cash'
                    AND [PaidAtUtc] IS NOT NULL
                    AND [Status] IN (N'Paid', N'Refunded')
                )
                """);

            table.HasCheckConstraint(
                "CK_Payments_RefundAudit",
                """
                (
                    [RefundedByUserId] IS NULL
                    AND [RefundReason] IS NULL
                )
                OR
                (
                    [RefundedByUserId] IS NOT NULL
                    AND [Status] = N'Refunded'
                    AND [RefundedAtUtc] IS NOT NULL
                    AND [PaidAtUtc] IS NOT NULL
                    AND [RefundedAtUtc] >= [PaidAtUtc]
                    AND [RefundReason] IS NOT NULL
                    AND LEN(LTRIM(RTRIM([RefundReason]))) > 0
                )
                """);
        });

        builder.Property(payment => payment.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(payment => payment.Method)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(payment => payment.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(payment => payment.TransactionReference)
            .HasMaxLength(200);

        builder.Property(payment => payment.FailureReason)
            .HasMaxLength(1000);

        builder.Property(payment => payment.RefundReason)
            .HasMaxLength(500);

        builder.HasIndex(payment => payment.TransactionReference)
            .IsUnique()
            .HasFilter(
                "[TransactionReference] IS NOT NULL");

        builder.HasIndex(payment => new
        {
            payment.OrderId,
            payment.CollectionRequestKey
        })
            .IsUnique()
            .HasFilter(
                "[CollectionRequestKey] IS NOT NULL")
            .HasDatabaseName(
                "UX_Payments_OrderId_CollectionRequestKey");

        builder.HasIndex(payment => payment.OrderId)
            .IsUnique()
            .HasFilter("[Status] = N'Paid'")
            .HasDatabaseName(
                "UX_Payments_OnePaidPaymentPerOrder");

        builder.HasOne(payment => payment.Order)
            .WithMany()
            .HasForeignKey(payment => payment.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(payment => payment.CollectedByUser)
            .WithMany()
            .HasForeignKey(payment => payment.CollectedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(payment => payment.RefundedByUser)
            .WithMany()
            .HasForeignKey(payment => payment.RefundedByUserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}