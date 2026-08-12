using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;

namespace RbacSystem.Infrastructure.Persistence.Configurations;

public class OtpVerificationConfiguration : IEntityTypeConfiguration<OtpVerification>
{
    public void Configure(EntityTypeBuilder<OtpVerification> builder)
    {
        builder.ToTable("otp_verifications", table =>
        {
            table.HasCheckConstraint("ck_otp_verifications_attempt_count", "attempt_count >= 0");
            table.HasCheckConstraint("ck_otp_verifications_resend_count", "resend_count >= 0");
            table.HasCheckConstraint("ck_otp_verifications_expiry", "expires_at > created_at");
        });

        builder.HasKey(otp => otp.Id);
        builder.Property(otp => otp.Id).HasColumnName("id").HasColumnType("varchar(36)").HasMaxLength(36);
        builder.Property(otp => otp.UserId).HasColumnName("user_id").HasColumnType("varchar(36)").HasMaxLength(36);
        builder.Property(otp => otp.Email).HasColumnName("email").HasColumnType("citext").HasMaxLength(255).IsRequired();
        builder.Property(otp => otp.CodeHash).HasColumnName("code_hash").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(otp => otp.Purpose)
            .HasColumnName("purpose")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .HasConversion(
                purpose => purpose == OtpPurpose.EmailVerification ? "email_verification" : "magic_login",
                value => value == "email_verification" ? OtpPurpose.EmailVerification : OtpPurpose.MagicLogin);
        builder.Property(otp => otp.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp with time zone");
        builder.Property(otp => otp.AttemptCount).HasColumnName("attempt_count").HasDefaultValue(0);
        builder.Property(otp => otp.ResendCount).HasColumnName("resend_count").HasDefaultValue(0);
        builder.Property(otp => otp.LastSentAt).HasColumnName("last_sent_at").HasColumnType("timestamp with time zone");
        builder.Property(otp => otp.UsedAt).HasColumnName("used_at").HasColumnType("timestamp with time zone");
        builder.Property(otp => otp.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamp with time zone");
        builder.Property(otp => otp.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        builder.Property(otp => otp.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");

        builder.HasOne(otp => otp.User)
            .WithMany()
            .HasForeignKey(otp => otp.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(otp => otp.UserId).HasDatabaseName("ix_otp_verifications_user_id");
        builder.HasIndex(otp => new { otp.Email, otp.Purpose, otp.CreatedAt }).HasDatabaseName("ix_otp_verifications_lookup");
        builder.HasIndex(otp => otp.ExpiresAt).HasDatabaseName("ix_otp_verifications_expires_at");
        builder.HasIndex(otp => new { otp.Email, otp.Purpose })
            .HasFilter("used_at IS NULL AND revoked_at IS NULL")
            .HasDatabaseName("ix_otp_verifications_active");
    }
}
