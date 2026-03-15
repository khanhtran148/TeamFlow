namespace TeamFlow.Domain.Entities;

public class WorkItemEmbedding
{
    public Guid WorkItemId { get; set; }
    // Embedding vector is stored as float[] and mapped to pgvector
    // It's null until AI service populates it
    public float[]? Embedding { get; set; }
    public string? Model { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public bool IsStale { get; set; } = true;

    // Navigation
    public WorkItem? WorkItem { get; set; }
}
