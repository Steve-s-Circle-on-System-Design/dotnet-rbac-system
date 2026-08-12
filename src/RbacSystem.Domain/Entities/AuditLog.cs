using System.Net;
using System.Text.Json;
using RbacSystem.Domain.Common;

namespace RbacSystem.Domain.Entities;

public class AuditLog
{
    public string Id { get; init; } = EntityId.New();
    public string? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public string? ResourceId { get; set; }
    public IPAddress? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public JsonDocument? Details { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
