namespace TeamFlow.Domain.Entities;

public sealed class NotificationPreference
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; } = true;
    public bool InAppEnabled { get; set; } = true;
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
}
