using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RbacSystem.Domain.Entities;
using RbacSystem.Domain.Enums;

namespace RbacSystem.Infrastructure.Persistence.Configurations;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("email_logs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Id).HasColumnName("id").HasColumnType("varchar(36)").HasMaxLength(36);
        builder.Property(log => log.UserId).HasColumnName("user_id").HasColumnType("varchar(36)").HasMaxLength(36);
        builder.Property(log => log.Recipient).HasColumnName("recipient").HasColumnType("citext").HasMaxLength(255).IsRequired();
        builder.Property(log => log.Subject).HasColumnName("subject").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(log => log.Template).HasColumnName("template").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(log => log.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .HasConversion(
                status => status == EmailStatus.Sent ? "sent" :
                    status == EmailStatus.Delivered ? "delivered" :
                    status == EmailStatus.Opened ? "opened" :
                    status == EmailStatus.Clicked ? "clicked" :
                    status == EmailStatus.Failed ? "failed" :
                    status == EmailStatus.Bounced ? "bounced" : "pending",
                value => value == "sent" ? EmailStatus.Sent :
                    value == "delivered" ? EmailStatus.Delivered :
                    value == "opened" ? EmailStatus.Opened :
                    value == "clicked" ? EmailStatus.Clicked :
                    value == "failed" ? EmailStatus.Failed :
                    value == "bounced" ? EmailStatus.Bounced : EmailStatus.Pending)
            .HasDefaultValue(EmailStatus.Pending);
        builder.Property(log => log.ProviderMessageId).HasColumnName("provider_message_id").HasColumnType("varchar(255)").HasMaxLength(255);
        builder.Property(log => log.DeliveryMetadata).HasColumnName("delivery_metadata").HasColumnType("jsonb");
        builder.Property(log => log.Error).HasColumnName("error").HasColumnType("text");
        builder.Property(log => log.SentAt).HasColumnName("sent_at").HasColumnType("timestamp with time zone");
        builder.Property(log => log.DeliveredAt).HasColumnName("delivered_at").HasColumnType("timestamp with time zone");
        builder.Property(log => log.OpenedAt).HasColumnName("opened_at").HasColumnType("timestamp with time zone");
        builder.Property(log => log.ClickedAt).HasColumnName("clicked_at").HasColumnType("timestamp with time zone");
        builder.Property(log => log.FailedAt).HasColumnName("failed_at").HasColumnType("timestamp with time zone");
        builder.Property(log => log.BouncedAt).HasColumnName("bounced_at").HasColumnType("timestamp with time zone");
        builder.Property(log => log.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone");
        builder.Property(log => log.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");

        builder.HasOne(log => log.User)
            .WithMany()
            .HasForeignKey(log => log.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(log => log.UserId).HasDatabaseName("ix_email_logs_user_id");
        builder.HasIndex(log => log.Status).HasDatabaseName("ix_email_logs_status");
        builder.HasIndex(log => log.ProviderMessageId).HasDatabaseName("ix_email_logs_provider_message_id");
        builder.HasIndex(log => log.CreatedAt).HasDatabaseName("ix_email_logs_created_at");
    }
}
