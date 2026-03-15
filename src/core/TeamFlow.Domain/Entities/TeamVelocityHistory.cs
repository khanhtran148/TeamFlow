using System.Text.Json;

namespace TeamFlow.Domain.Entities;

public class TeamVelocityHistory
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid SprintId { get; set; }
    public int PlannedPoints { get; set; }
    public int CompletedPoints { get; set; }
    public int Velocity { get; set; }
    public double? Velocity3SprintAvg { get; set; }
    public double? Velocity6SprintAvg { get; set; }
    public string? VelocityTrend { get; set; } // Increasing, Decreasing, Stable
    public double? AiAdjustedVelocity { get; set; } // null until AI populates
    public JsonDocument? ConfidenceInterval { get; set; } // {lower, upper}
    public DateTime RecordedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public Project? Project { get; set; }
    public Sprint? Sprint { get; set; }
}
