using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;

namespace RbacSystem.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table =>
        {
            table.HasCheckConstraint("ck_users_failed_login_attempts", "failed_login_attempts >= 0");
            table.HasCheckConstraint("ck_users_token_version", "token_version >= 0");
        });

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).HasColumnName("id").HasColumnType("varchar(36)").HasMaxLength(36);
        builder.Property(user => user.Email).HasColumnName("email").HasColumnType("citext").HasMaxLength(255).IsRequired();
        builder.Property(user => user.Name).HasColumnName("name").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(user => user.PasswordHash).HasColumnName("password_hash").HasColumnType("varchar(255)").HasMaxLength(255);
        builder.Property(user => user.Role)
            .HasColumnName("role")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .HasConversion(
                role => role == UserRole.Admin ? "admin" : "user",
                value => value == "admin" ? UserRole.Admin : UserRole.User)
            .HasDefaultValue(UserRole.User);
        builder.Property(user => user.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .HasConversion(
                status => status == UserStatus.Active ? "active" :
                    status == UserStatus.Inactive ? "inactive" :
                    status == UserStatus.Suspended ? "suspended" : "pending_verification",
                value => value == "active" ? UserStatus.Active :
                    value == "inactive" ? UserStatus.Inactive :
                    value == "suspended" ? UserStatus.Suspended : UserStatus.PendingVerification)
            .HasDefaultValue(UserStatus.PendingVerification);
        builder.Property(user => user.EmailVerifiedAt).HasColumnName("email_verified_at").HasColumnType("timestamp with time zone");
        builder.Property(user => user.LastLoginAt).HasColumnName("last_login_at").HasColumnType("timestamp with time zone");
        builder.Property(user => user.LastLoginIp).HasColumnName("last_login_ip").HasColumnType("inet");
        builder.Property(user => user.FailedLoginAttempts).HasColumnName("failed_login_attempts").HasDefaultValue(0);
        builder.Property(user => user.LockoutEnd).HasColumnName("lockout_end").HasColumnType("timestamp with time zone");
        builder.Property(user => user.ProfilePicture).HasColumnName("profile_picture").HasColumnType("text");
        builder.Property(user => user.Provider).HasColumnName("provider").HasColumnType("varchar(50)").HasMaxLength(50);
        builder.Property(user => user.ProviderId).HasColumnName("provider_id").HasColumnType("varchar(255)").HasMaxLength(255);
        builder.Property(user => user.TokenVersion).HasColumnName("token_version").HasDefaultValue(0);
        builder.Property(user => user.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        builder.Property(user => user.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
        builder.Property(user => user.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamp with time zone");
        builder.Ignore(user => user.IsEmailVerified);

        // Soft-deleted users are excluded from every query by default, so no feature
        // has to remember to filter them out. Administrative flows that genuinely
        // need deleted rows opt back in with IgnoreQueryFilters().
        builder.HasQueryFilter(user => user.DeletedAt == null);

        builder.HasIndex(user => user.Email).IsUnique().HasDatabaseName("ux_users_email");
        builder.HasIndex(user => new { user.Provider, user.ProviderId })
            .IsUnique()
            .HasFilter("provider IS NOT NULL AND provider_id IS NOT NULL")
            .HasDatabaseName("ux_users_provider_identity");
        builder.HasIndex(user => user.Status).HasDatabaseName("ix_users_status");
        builder.HasIndex(user => user.DeletedAt).HasDatabaseName("ix_users_deleted_at");
    }
}
