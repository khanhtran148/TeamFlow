using System.Text.Json;

namespace TeamFlow.Domain.Entities;

/// <summary>
/// Partitioned event log for AI training. Partition by occurred_at.
/// </summary>
public class DomainEvent
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public Guid? ActorId { get; set; }
    public string ActorType { get; set; } = "User";
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
    public JsonDocument Metadata { get; set; } = JsonDocument.Parse("{}");
    public DateTime OccurredAt { get; set; }
    public DateTime RecordedAt { get; protected set; } = DateTime.UtcNow;
    public int SchemaVersion { get; set; } = 1;
    public Guid? SessionId { get; set; }

    // Navigation
    public User? Actor { get; set; }
}
