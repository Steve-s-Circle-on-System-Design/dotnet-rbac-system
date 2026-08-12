using System.Text.Json;
using RbacSystem.Domain.Common;
using RbacSystem.Domain.Enums;

namespace RbacSystem.Domain.Entities;

public class EmailLog
{
    public string Id { get; init; } = EntityId.New();
    public string? UserId { get; set; }
    public User? User { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public string? ProviderMessageId { get; set; }
    public JsonDocument? DeliveryMetadata { get; set; }
    public string? Error { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? BouncedAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
