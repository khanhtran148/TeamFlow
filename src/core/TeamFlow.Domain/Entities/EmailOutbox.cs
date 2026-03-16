using System.Text.Json;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public sealed class EmailOutbox
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public string RecipientEmail { get; set; } = string.Empty;
    public Guid? RecipientId { get; set; }
    public string TemplateType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public JsonDocument BodyJson { get; set; } = JsonDocument.Parse("{}");
    public EmailStatus Status { get; set; } = EmailStatus.Pending;
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public DateTime? NextRetryAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }

    // Navigation
    public User? Recipient { get; set; }
}
