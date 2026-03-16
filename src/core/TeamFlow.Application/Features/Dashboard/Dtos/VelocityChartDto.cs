namespace TeamFlow.Application.Features.Dashboard.Dtos;

public sealed record VelocityChartDto(IReadOnlyList<VelocitySprintDto> Sprints);

public sealed record VelocitySprintDto(
    Guid SprintId,
    string SprintName,
    decimal PlannedPoints,
    decimal CompletedPoints,
    decimal Velocity,
    decimal Avg3Sprint,
    decimal Avg6Sprint
);
