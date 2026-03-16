using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.WorkItems;

public sealed record WorkItemDto(
    Guid Id,
    Guid ProjectId,
    Guid? ParentId,
    WorkItemType Type,
    string Title,
    string? Description,
    WorkItemStatus Status,
    Priority? Priority,
    decimal? EstimationValue,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTime? AssignedAt,
    Guid? SprintId,
    Guid? ReleaseId,
    int ChildCount,
    int LinkCount,
    int SortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public sealed record WorkItemSummaryDto(
    Guid Id,
    WorkItemType Type,
    string Title,
    WorkItemStatus Status,
    Priority? Priority,
    Guid? AssigneeId
);
