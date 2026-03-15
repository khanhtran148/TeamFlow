using System.Text.Json;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public class RetroCard
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid AuthorId { get; set; } // Never exposed in API when anonymous
    public RetroCardCategory Category { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsDiscussed { get; set; } = false;
    public double? Sentiment { get; set; } // AI-analyzed
    public JsonDocument ThemeTags { get; set; } = JsonDocument.Parse("[]");
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public RetroSession? Session { get; set; }
    public User? Author { get; set; }
    public ICollection<RetroVote> Votes { get; set; } = [];
    public ICollection<RetroActionItem> ActionItems { get; set; } = [];
}
