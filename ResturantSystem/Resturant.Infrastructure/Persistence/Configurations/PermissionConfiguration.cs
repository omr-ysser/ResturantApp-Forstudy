using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration
    : BaseEntityConfiguration<Permission>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.Property(permission => permission.Key)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(permission => permission.NormalizedKey)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(permission => permission.NormalizedKey)
            .IsUnique();

        builder.Property(permission => permission.DisplayName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Navigation(permission => permission.RolePermissions)
            .HasField("_rolePermissions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
