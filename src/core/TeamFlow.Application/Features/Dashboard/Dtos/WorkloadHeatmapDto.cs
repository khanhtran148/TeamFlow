namespace TeamFlow.Application.Features.Dashboard.Dtos;

public sealed record WorkloadHeatmapDto(IReadOnlyList<WorkloadMemberDto> Members);

public sealed record WorkloadMemberDto(
    Guid UserId,
    string Name,
    int AssignedCount,
    int InProgressCount,
    decimal PointsAssigned
);
