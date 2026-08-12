using System.Net;
using RbacSystem.Domain.Common;

namespace RbacSystem.Domain.Entities;

public class RefreshToken
{
    public string Id { get; init; } = EntityId.New();
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public string TokenFamily { get; set; } = EntityId.New();
    public string? RotatedFromId { get; set; }
    public RefreshToken? RotatedFrom { get; set; }
    public ICollection<RefreshToken> ReplacementTokens { get; set; } = [];
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
    public string? UserAgent { get; set; }
    public IPAddress? IpAddress { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
