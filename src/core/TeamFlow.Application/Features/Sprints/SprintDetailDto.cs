using TeamFlow.Application.Features.WorkItems;

namespace TeamFlow.Application.Features.Sprints;

public sealed record SprintDetailDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Goal,
    DateOnly? StartDate,
    DateOnly? EndDate,
    Domain.Enums.SprintStatus Status,
    int TotalPoints,
    int CompletedPoints,
    int ItemCount,
    float? CapacityUtilization,
    DateTime CreatedAt,
    IReadOnlyList<WorkItemDto> Items,
    IReadOnlyList<CapacityEntryDto> Capacity
);

public sealed record CapacityEntryDto(
    Guid MemberId,
    string MemberName,
    int CapacityPoints,
    int AssignedPoints
);
