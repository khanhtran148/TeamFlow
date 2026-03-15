using System.Text.Json;

namespace TeamFlow.Domain.Entities;

public class JobExecutionMetric
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public string JobType { get; set; } = string.Empty;
    public Guid JobRunId { get; set; }
    public string Status { get; set; } = string.Empty; // Running, Success, Failed, Cancelled
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationMs { get; set; }
    public int RecordsProcessed { get; set; } = 0;
    public int RecordsFailed { get; set; } = 0;
    public string? ErrorMessage { get; set; }
    public JsonDocument Metadata { get; set; } = JsonDocument.Parse("{}");
}
