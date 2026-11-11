using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration
    : BaseEntityConfiguration<OrderItem>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", table =>
        {
            table.HasCheckConstraint(
                "CK_OrderItems_Quantity",
                "[Quantity] > 0");

            table.HasCheckConstraint(
                "CK_OrderItems_UnitPrice",
                "[UnitPrice] > 0");
        });

        builder.Property(item => item.ItemName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(item => item.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(item => item.Notes)
            .HasMaxLength(1000);

        builder.Ignore(item => item.TotalPrice);

        builder.HasOne(item => item.MenuItem)
            .WithMany()
            .HasForeignKey(item => item.MenuItemId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
