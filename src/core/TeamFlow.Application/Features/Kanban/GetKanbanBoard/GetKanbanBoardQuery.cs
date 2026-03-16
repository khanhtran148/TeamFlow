using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Kanban.GetKanbanBoard;

public sealed record GetKanbanBoardQuery(
    Guid ProjectId,
    Guid? AssigneeId,
    WorkItemType? Type,
    Priority? Priority,
    Guid? SprintId,
    Guid? ReleaseId,
    string? Swimlane  // "assignee" or "epic"
) : IRequest<Result<KanbanBoardDto>>;

public sealed record KanbanBoardDto(
    Guid ProjectId,
    IEnumerable<KanbanColumnDto> Columns,
    IEnumerable<KanbanSwimlaneDto>? Swimlanes = null
);

public sealed record KanbanSwimlaneDto(
    string Key,
    string Label,
    IEnumerable<KanbanColumnDto> Columns
);

public sealed record KanbanColumnDto(
    WorkItemStatus Status,
    int ItemCount,
    IEnumerable<KanbanItemDto> Items
);

public sealed record KanbanItemDto(
    Guid Id,
    WorkItemType Type,
    string Title,
    WorkItemStatus Status,
    Priority? Priority,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTime? AssignedAt,
    Guid? ParentId,
    string? ParentTitle,
    bool IsBlocked,
    Guid? ReleaseId
);
