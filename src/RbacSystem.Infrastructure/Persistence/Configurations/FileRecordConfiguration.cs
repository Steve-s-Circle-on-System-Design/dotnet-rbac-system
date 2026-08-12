using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RbacSystem.Domain.Entities;

namespace RbacSystem.Infrastructure.Persistence.Configurations;

public class FileRecordConfiguration : IEntityTypeConfiguration<FileRecord>
{
    public void Configure(EntityTypeBuilder<FileRecord> builder)
    {
        builder.ToTable("files", table =>
        {
            table.HasCheckConstraint("ck_files_file_size", "file_size >= 0");
            table.HasCheckConstraint("ck_files_width", "width IS NULL OR width > 0");
            table.HasCheckConstraint("ck_files_height", "height IS NULL OR height > 0");
        });

        builder.HasKey(file => file.Id);
        builder.Property(file => file.Id).HasColumnName("id").HasColumnType("varchar(36)").HasMaxLength(36);
        builder.Property(file => file.UserId).HasColumnName("user_id").HasColumnType("varchar(36)").HasMaxLength(36).IsRequired();
        builder.Property(file => file.FileName).HasColumnName("file_name").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(file => file.FileSize).HasColumnName("file_size").HasColumnType("bigint");
        builder.Property(file => file.MimeType).HasColumnName("mime_type").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
        builder.Property(file => file.CloudinaryPublicId).HasColumnName("cloudinary_public_id").HasColumnType("varchar(255)").HasMaxLength(255).IsRequired();
        builder.Property(file => file.SecureUrl).HasColumnName("secure_url").HasColumnType("text").IsRequired();
        builder.Property(file => file.Format).HasColumnName("format").HasColumnType("varchar(50)").HasMaxLength(50);
        builder.Property(file => file.Width).HasColumnName("width");
        builder.Property(file => file.Height).HasColumnName("height");
        builder.Property(file => file.UploadedAt).HasColumnName("uploaded_at").HasColumnType("timestamp with time zone");
        builder.Property(file => file.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");

        builder.HasOne(file => file.User)
            .WithMany()
            .HasForeignKey(file => file.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(file => file.UserId).HasDatabaseName("ix_files_user_id");
        builder.HasIndex(file => file.CloudinaryPublicId).IsUnique().HasDatabaseName("ux_files_cloudinary_public_id");
        builder.HasIndex(file => file.UploadedAt).HasDatabaseName("ix_files_uploaded_at");
    }
}
