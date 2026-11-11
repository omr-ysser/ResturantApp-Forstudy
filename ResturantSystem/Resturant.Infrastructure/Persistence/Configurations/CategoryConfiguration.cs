using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration
    : BaseEntityConfiguration<Category>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.Property(category => category.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(category => category.Name)
            .IsUnique();

        builder.Property(category => category.Description)
            .HasMaxLength(500);

        builder.Navigation(category => category.MenuItems)
            .HasField("_menuItems")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
