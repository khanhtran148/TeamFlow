using System.Text.Json;

namespace TeamFlow.Domain.Entities;

public class AiInteraction
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public string FeatureType { get; set; } = string.Empty; // EstimationSuggest, TaskBreakdown, etc.
    public Guid? WorkItemId { get; set; }
    public Guid? SprintId { get; set; }
    public string? ModelVersion { get; set; }
    public JsonDocument InputContext { get; set; } = JsonDocument.Parse("{}");
    public JsonDocument AiOutput { get; set; } = JsonDocument.Parse("{}");
    public string UserAction { get; set; } = string.Empty; // Accepted, Modified, Rejected, Ignored
    public JsonDocument? UserModified { get; set; }
    public Guid ActorId { get; set; }
    public int? LatencyMs { get; set; }
    public DateTime OccurredAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public WorkItem? WorkItem { get; set; }
    public Sprint? Sprint { get; set; }
    public User? Actor { get; set; }
}
