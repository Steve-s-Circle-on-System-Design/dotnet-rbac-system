using RbacSystem.Domain.Common;

namespace RbacSystem.Domain.Entities;

public class FileRecord
{
    public string Id { get; init; } = EntityId.New();
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string CloudinaryPublicId { get; set; } = string.Empty;
    public string SecureUrl { get; set; } = string.Empty;
    public string? Format { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
