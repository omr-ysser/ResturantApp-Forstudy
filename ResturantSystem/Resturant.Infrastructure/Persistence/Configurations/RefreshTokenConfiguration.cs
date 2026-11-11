using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Resturant.Domain.Entities;

namespace Resturant.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration
    : BaseEntityConfiguration<RefreshToken>
{
    protected override void ConfigureEntity(
        EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.Property(token => token.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.Property(token => token.ExpiresAtUtc)
            .IsRequired();

        builder.Property(token => token.SessionId)
            .IsRequired(false);

        builder.HasOne(token => token.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(token => token.Session)
            .WithMany(session => session.RefreshTokens)
            .HasForeignKey(token => new
            {
                token.SessionId,
                token.UserId
            })
            .HasPrincipalKey(session => new
            {
                session.Id,
                session.UserId
            })
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(token => token.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(token => new
        {
            token.UserId,
            token.ExpiresAtUtc
        });

        builder.HasIndex(token => new
        {
            token.SessionId,
            token.UserId,
            token.ExpiresAtUtc
        });
    }
}