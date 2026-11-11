using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration
    : BaseEntityConfiguration<User>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.Property(user => user.FullName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(user => user.NormalizedEmail)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(user => user.IsActive)
            .IsRequired();

        builder.Navigation(user => user.UserRoles)
            .HasField("_userRoles")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(user => user.RefreshTokens)
            .HasField("_refreshTokens")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
