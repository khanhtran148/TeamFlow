using System.Text.Json;

namespace TeamFlow.Domain.Entities;

/// <summary>
/// Append-only audit log for work item changes. Never update or delete.
/// </summary>
public class WorkItemHistory
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid WorkItemId { get; set; }
    public Guid? ActorId { get; set; }
    public string ActorType { get; set; } = "User"; // User, System, AI
    public string ActionType { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public JsonDocument Metadata { get; set; } = JsonDocument.Parse("{}");
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public WorkItem? WorkItem { get; set; }
    public User? Actor { get; set; }
}
