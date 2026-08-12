using System.Net;
using RbacSystem.Domain.Common;
using RbacSystem.Domain.Enums;

namespace RbacSystem.Domain.Entities;

public class User
{
    public string Id { get; init; } = EntityId.New();
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public UserStatus Status { get; set; } = UserStatus.PendingVerification;
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public IPAddress? LastLoginIp { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public string? ProfilePicture { get; set; }
    public string? Provider { get; set; }
    public string? ProviderId { get; set; }
    public int TokenVersion { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public bool IsEmailVerified => EmailVerifiedAt.HasValue;
}
