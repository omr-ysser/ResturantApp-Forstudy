using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Configurations;

public sealed class AuthenticationSessionConfiguration
    : BaseEntityConfiguration<AuthenticationSession>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<AuthenticationSession> builder)
    {
        builder.ToTable(
            "AuthenticationSessions",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_AuthenticationSessions_Expiry",
                    "[ExpiresAtUtc] > [StartedAtUtc]");

                table.HasCheckConstraint(
                    "CK_AuthenticationSessions_RefreshCount",
                    "[RefreshCount] >= 0");

                table.HasCheckConstraint(
                    "CK_AuthenticationSessions_Revocation",
                    "([RevokedAtUtc] IS NULL " +
                    "AND [RevocationReason] IS NULL) OR " +
                    "([RevokedAtUtc] IS NOT NULL " +
                    "AND [RevocationReason] IS NOT NULL)");
            });

        builder.Property(session => session.StartedAtUtc)
            .IsRequired();

        builder.Property(session => session.LastUsedAtUtc)
            .IsRequired();

        builder.Property(session => session.ExpiresAtUtc)
            .IsRequired();

        builder.Property(session => session.RefreshCount)
            .IsRequired();

        builder.Property(session => session.RevocationReason)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.HasOne(session => session.User)
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasAlternateKey(session => new
        {
            session.Id,
            session.UserId
        });

        builder.Navigation(session => session.RefreshTokens)
            .HasField("_refreshTokens")
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);

        builder.HasIndex(session => new
        {
            session.UserId,
            session.RevokedAtUtc,
            session.ExpiresAtUtc
        });
    }
}
