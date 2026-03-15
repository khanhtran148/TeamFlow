namespace TeamFlow.Application.Features.Sprints;

public sealed record BurndownDto(
    Guid SprintId,
    IReadOnlyList<IdealPointDto> IdealLine,
    IReadOnlyList<ActualPointDto> ActualLine
);

public sealed record IdealPointDto(DateOnly Date, int Points);

public sealed record ActualPointDto(
    DateOnly Date,
    int RemainingPoints,
    int CompletedPoints,
    int AddedPoints
);
