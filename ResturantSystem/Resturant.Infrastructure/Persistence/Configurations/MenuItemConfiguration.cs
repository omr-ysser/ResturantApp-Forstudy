using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Configurations;

public sealed class MenuItemConfiguration
    : BaseEntityConfiguration<MenuItem>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems", table =>
        {
            table.HasCheckConstraint(
                "CK_MenuItems_Price",
                "[Price] > 0");
        });

        builder.Property(item => item.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(item => item.Description)
            .HasMaxLength(1000);

        builder.Property(item => item.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(item => new
        {
            item.CategoryId,
            item.Name
        }).IsUnique();

        builder.HasOne(item => item.Category)
            .WithMany(category => category.MenuItems)
            .HasForeignKey(item => item.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
