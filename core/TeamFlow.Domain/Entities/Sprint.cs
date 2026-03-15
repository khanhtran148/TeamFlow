using System.Text.Json;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public class Sprint
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public SprintStatus Status { get; set; } = SprintStatus.Planning;
    public JsonDocument? CapacityJson { get; set; } // {member_id: points}
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public Project? Project { get; set; }
    public ICollection<WorkItem> WorkItems { get; set; } = [];
    public ICollection<SprintSnapshot> Snapshots { get; set; } = [];
    public ICollection<BurndownDataPoint> BurndownDataPoints { get; set; } = [];
}
