using System.Text.Json;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public class RetroSession
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public string Name { get; set; } = "Retro";
    public Guid? SprintId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid FacilitatorId { get; set; }
    public string AnonymityMode { get; set; } = Enums.RetroAnonymityModes.Public;
    public RetroSessionStatus Status { get; set; } = RetroSessionStatus.Draft;
    public JsonDocument? AiSummary { get; set; }
    public JsonDocument? ColumnsConfig { get; set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public Sprint? Sprint { get; set; }
    public Project? Project { get; set; }
    public User? Facilitator { get; set; }
    public ICollection<RetroCard> Cards { get; set; } = [];
    public ICollection<RetroActionItem> ActionItems { get; set; } = [];
}
