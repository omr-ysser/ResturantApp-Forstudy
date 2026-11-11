using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Configurations;

public sealed class OrderCreationReceiptConfiguration
    : BaseEntityConfiguration<OrderCreationReceipt>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<OrderCreationReceipt> builder)
    {
        builder.ToTable("OrderCreationReceipts");

        builder.Property(receipt => receipt.RequestHash)
            .HasColumnType("char(64)")
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(receipt => new
        {
            receipt.UserId,
            receipt.RequestKey
        })
            .IsUnique()
            .HasDatabaseName(
                "UX_OrderCreationReceipts_UserId_RequestKey");

        builder.HasOne(receipt => receipt.Order)
            .WithOne(order => order.CreationReceipt)
            .HasForeignKey<OrderCreationReceipt>(
                receipt => receipt.OrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(receipt => receipt.User)
            .WithMany()
            .HasForeignKey(receipt => receipt.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
