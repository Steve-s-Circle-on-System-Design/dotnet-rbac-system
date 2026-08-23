using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RbacSystem.Domain.Entities;

namespace RbacSystem.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", table =>
        {
            table.HasCheckConstraint("ck_refresh_tokens_expiry", "expires_at > created_at");
            table.HasCheckConstraint("ck_refresh_tokens_rotation", "rotated_from_id IS NULL OR rotated_from_id <> id");
        });

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).HasColumnName("id").HasColumnType("varchar(36)").HasMaxLength(36);
        builder.Property(token => token.UserId).HasColumnName("user_id").HasColumnType("varchar(36)").HasMaxLength(36).IsRequired();
        builder.Property(token => token.TokenHash).HasColumnName("token_hash").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(token => token.TokenFamily).HasColumnName("token_family").HasColumnType("varchar(36)").HasMaxLength(36).IsRequired();
        builder.Property(token => token.RotatedFromId).HasColumnName("rotated_from_id").HasColumnType("varchar(36)").HasMaxLength(36);
        builder.Property(token => token.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone");
        builder.Property(token => token.UsedAt).HasColumnName("used_at").HasColumnType("timestamp with time zone");
        builder.Property(token => token.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamp with time zone");
        builder.Property(token => token.RevokeReason).HasColumnName("revoke_reason").HasColumnType("varchar(255)").HasMaxLength(255);
        builder.Property(token => token.UserAgent).HasColumnName("user_agent").HasColumnType("varchar(500)").HasMaxLength(500);
        builder.Property(token => token.IpAddress).HasColumnName("ip_address").HasColumnType("inet");
        builder.Property(token => token.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");

        // Matches the soft-delete filter on User. Without it EF warns that this
        // required navigation can resolve to a filtered-out principal, and a deleted
        // user's rows would stay queryable through this entity.
        builder.HasQueryFilter(entity => entity.User.DeletedAt == null);

        builder.HasOne(token => token.User)
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(token => token.RotatedFrom)
            .WithMany(token => token.ReplacementTokens)
            .HasForeignKey(token => token.RotatedFromId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(token => token.TokenHash).IsUnique().HasDatabaseName("ux_refresh_tokens_token_hash");
        builder.HasIndex(token => token.UserId).HasDatabaseName("ix_refresh_tokens_user_id");
        builder.HasIndex(token => token.TokenFamily).HasDatabaseName("ix_refresh_tokens_token_family");
        builder.HasIndex(token => token.ExpiresAt).HasDatabaseName("ix_refresh_tokens_expires_at");
        builder.HasIndex(token => token.RotatedFromId).HasDatabaseName("ix_refresh_tokens_rotated_from_id");
    }
}
