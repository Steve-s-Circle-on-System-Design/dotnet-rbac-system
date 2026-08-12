using RbacSystem.Domain.Common;

namespace RbacSystem.Domain.Entities;

public class PasswordResetToken
{
    public string Id { get; init; } = EntityId.New();
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
