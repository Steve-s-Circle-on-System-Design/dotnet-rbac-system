using RbacSystem.Domain.Common;
using RbacSystem.Domain.Enums;

namespace RbacSystem.Domain.Entities;

public class OtpVerification
{
    public string Id { get; init; } = EntityId.New();
    public string? UserId { get; set; }
    public User? User { get; set; }
    public string Email { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int AttemptCount { get; set; }
    public int ResendCount { get; set; }
    public DateTime LastSentAt { get; set; } = DateTime.UtcNow;
    public DateTime? UsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
