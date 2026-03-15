using System.Text.Json;

namespace TeamFlow.Domain.Entities;

public sealed class SprintSnapshot
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid SprintId { get; set; }
    public string SnapshotType { get; set; } = string.Empty; // OnStart, Daily, OnClose
    public bool IsFinal { get; set; } = false;
    public JsonDocument Payload { get; set; } = JsonDocument.Parse("{}");
    public DateTime CapturedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public Sprint? Sprint { get; set; }
}
