using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RbacSystem.Domain.Entities;

namespace RbacSystem.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).HasColumnName("id").HasColumnType("varchar(36)").HasMaxLength(36);
        builder.Property(log => log.UserId).HasColumnName("user_id").HasColumnType("varchar(36)").HasMaxLength(36);
        builder.Property(log => log.Action).HasColumnName("action").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(log => log.Resource).HasColumnName("resource").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(log => log.ResourceId).HasColumnName("resource_id").HasColumnType("varchar(36)").HasMaxLength(36);
        builder.Property(log => log.IpAddress).HasColumnName("ip_address").HasColumnType("inet");
        builder.Property(log => log.UserAgent).HasColumnName("user_agent").HasColumnType("varchar(500)").HasMaxLength(500);
        builder.Property(log => log.Details).HasColumnName("details").HasColumnType("jsonb");
        builder.Property(log => log.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");

        builder.HasOne(log => log.User)
            .WithMany()
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(log => log.UserId).HasDatabaseName("ix_audit_logs_user_id");
        builder.HasIndex(log => log.Action).HasDatabaseName("ix_audit_logs_action");
        builder.HasIndex(log => new { log.Resource, log.ResourceId }).HasDatabaseName("ix_audit_logs_resource");
        builder.HasIndex(log => log.CreatedAt).HasDatabaseName("ix_audit_logs_created_at");
    }
}
