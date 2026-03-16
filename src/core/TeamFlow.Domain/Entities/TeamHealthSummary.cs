using System.Text.Json;

namespace TeamFlow.Domain.Entities;

public sealed class TeamHealthSummary
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public JsonDocument SummaryData { get; set; } = JsonDocument.Parse("{}");
    public DateTime GeneratedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public Project? Project { get; set; }
}
