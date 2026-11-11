using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration
    : BaseEntityConfiguration<Role>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.Property(role => role.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(role => role.NormalizedName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(role => role.NormalizedName)
            .IsUnique();

        builder.Navigation(role => role.UserRoles)
            .HasField("_userRoles")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(role => role.RolePermissions)
            .HasField("_rolePermissions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
