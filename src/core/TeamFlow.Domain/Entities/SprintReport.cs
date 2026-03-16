using System.Text.Json;

namespace TeamFlow.Domain.Entities;

public sealed class SprintReport
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid SprintId { get; set; }
    public Guid ProjectId { get; set; }
    public JsonDocument ReportData { get; set; } = JsonDocument.Parse("{}");
    public DateTime GeneratedAt { get; protected set; } = DateTime.UtcNow;
    public string GeneratedBy { get; set; } = "System";

    // Navigation
    public Sprint? Sprint { get; set; }
    public Project? Project { get; set; }
}
