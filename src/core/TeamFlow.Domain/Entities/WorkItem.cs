using System.Text.Json;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public class WorkItem : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid? ParentId { get; set; }
    public WorkItemType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkItemStatus Status { get; set; } = WorkItemStatus.ToDo;
    public Priority? Priority { get; set; }

    // Estimation (flexible, AI-ready)
    public decimal? EstimationValue { get; set; }
    public string EstimationUnit { get; set; } = "StoryPoint";
    public double? EstimationConfidence { get; set; } // 0.0-1.0, null until AI
    public string? EstimationSource { get; set; }     // Human, AI, Hybrid
    public JsonDocument EstimationHistory { get; set; } = JsonDocument.Parse("[]");

    // Assignments
    public Guid? AssigneeId { get; set; }
    public Guid? SprintId { get; set; }
    public Guid? ReleaseId { get; set; }

    // Retrospective link
    public Guid? RetroActionItemId { get; set; }

    // Flexible fields
    public JsonDocument CustomFields { get; set; } = JsonDocument.Parse("{}");
    public JsonDocument AiMetadata { get; set; } = JsonDocument.Parse("""
        {
          "suggested_epic_id": null,
          "risk_score": null,
          "complexity_indicators": [],
          "similar_item_ids": [],
          "auto_generated": false,
          "sprint_fit_probability": null,
          "stale_flag": false
        }
        """);
    public JsonDocument ExternalRefs { get; set; } = JsonDocument.Parse("{}");

    // Search vector is managed by the DB, not mapped here for writes
    // public NpgsqlTsVector? SearchVector { get; set; }

    // Soft delete
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public Project? Project { get; set; }
    public WorkItem? Parent { get; set; }
    public ICollection<WorkItem> Children { get; set; } = [];
    public User? Assignee { get; set; }
    public Sprint? Sprint { get; set; }
    public Release? Release { get; set; }
    public RetroActionItem? RetroActionItem { get; set; }
    public ICollection<WorkItemHistory> Histories { get; set; } = [];
    public ICollection<WorkItemLink> SourceLinks { get; set; } = [];
    public ICollection<WorkItemLink> TargetLinks { get; set; } = [];
    public WorkItemEmbedding? Embedding { get; set; }
}
