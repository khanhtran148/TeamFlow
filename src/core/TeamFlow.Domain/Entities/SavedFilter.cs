using System.Text.Json;

namespace TeamFlow.Domain.Entities;

public sealed class SavedFilter : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public JsonDocument FilterJson { get; set; } = JsonDocument.Parse("{}");
    public bool IsDefault { get; set; }

    // Navigation
    public User? User { get; set; }
    public Project? Project { get; set; }
}
