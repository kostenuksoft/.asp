using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Api.Data.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RefreshTokens");
        builder.HasKey(token => token.RefreshTokenId);

        builder.Property(token => token.TokenHash)
            .HasMaxLength(64)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.Property(token => token.ReplacedByTokenHash)
            .HasMaxLength(64)
            .IsUnicode(false);
        builder.Property(token => token.RevokedReason)
            .HasMaxLength(100);

        builder.HasIndex(token => new { token.UserId, token.ExpiresAt });

        builder.HasOne<ApplicationUser>()
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
