using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Sprints;

public sealed record SprintDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Goal,
    DateOnly? StartDate,
    DateOnly? EndDate,
    SprintStatus Status,
    int TotalPoints,
    int CompletedPoints,
    int ItemCount,
    float? CapacityUtilization,
    DateTime CreatedAt
);
