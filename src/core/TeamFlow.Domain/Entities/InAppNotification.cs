namespace TeamFlow.Domain.Entities;

public sealed class InAppNotification
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid RecipientId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public User? Recipient { get; set; }
}
