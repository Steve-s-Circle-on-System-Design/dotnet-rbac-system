using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RbacSystem.Domain.Entities;

namespace RbacSystem.Infrastructure.Persistence.Configurations;

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_resets", table =>
            table.HasCheckConstraint("ck_password_resets_expiry", "expires_at > created_at"));

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).HasColumnName("id").HasColumnType("varchar(36)").HasMaxLength(36);
        builder.Property(token => token.UserId).HasColumnName("user_id").HasColumnType("varchar(36)").HasMaxLength(36).IsRequired();
        builder.Property(token => token.TokenHash).HasColumnName("token_hash").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(token => token.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone");
        builder.Property(token => token.UsedAt).HasColumnName("used_at").HasColumnType("timestamp with time zone");
        builder.Property(token => token.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamp with time zone");
        builder.Property(token => token.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");

        builder.HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(token => token.TokenHash).IsUnique().HasDatabaseName("ux_password_resets_token_hash");
        builder.HasIndex(token => token.ExpiresAt).HasDatabaseName("ix_password_resets_expires_at");
        builder.HasIndex(token => new { token.UserId, token.ExpiresAt })
            .HasFilter("used_at IS NULL AND revoked_at IS NULL")
            .HasDatabaseName("ix_password_resets_active");
    }
}
