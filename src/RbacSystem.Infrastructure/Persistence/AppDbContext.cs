using Microsoft.EntityFrameworkCore;
using RbacSystem.Domain.Entities;

namespace RbacSystem.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<FileRecord> Files => Set<FileRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.HasPostgresExtension("citext");
        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override int SaveChanges()
    {
        SetUpdatedTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetUpdatedTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetUpdatedTimestamps()
    {
        // Keep updated_at consistent even when callers forget to set it explicitly.
        DateTime now = DateTime.UtcNow;

        foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            switch (entry.Entity)
            {
                case User user:
                    user.UpdatedAt = now;
                    break;
                case OtpVerification otpVerification:
                    otpVerification.UpdatedAt = now;
                    break;
                case EmailLog emailLog:
                    emailLog.UpdatedAt = now;
                    break;
                case FileRecord file:
                    file.UpdatedAt = now;
                    break;
                default:
                    break;
            }
        }
    }
}
