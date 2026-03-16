using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Releases;

public sealed record ReleaseDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string? ReleaseNotes,
    DateOnly? ReleaseDate,
    ReleaseStatus Status,
    bool NotesLocked,
    bool IsOverdue,
    ReleaseProgressDto Progress,
    IReadOnlyList<ReleaseGroupDto> ByEpic,
    IReadOnlyList<ReleaseGroupDto> ByAssignee,
    IReadOnlyList<ReleaseGroupDto> BySprint,
    DateTime CreatedAt
);

public sealed record ReleaseProgressDto(
    int TotalItems,
    int DoneItems,
    int InProgressItems,
    int ToDoItems,
    decimal TotalPoints,
    decimal DonePoints,
    decimal InProgressPoints,
    decimal ToDoPoints
);

public sealed record ReleaseGroupDto(
    string GroupName,
    Guid? GroupId,
    int ItemCount,
    int DoneCount
);

public sealed record ShipReleaseResultDto(
    bool Shipped,
    IReadOnlyList<IncompleteItemDto>? IncompleteItems
);

public sealed record IncompleteItemDto(
    Guid Id,
    string Title,
    WorkItemStatus Status
);
