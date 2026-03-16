namespace TeamFlow.Application.Features.Dashboard.Dtos;

public sealed record DashboardSummaryDto(
    Guid? ActiveSprintId,
    string? ActiveSprintName,
    int TotalItems,
    int OpenItems,
    double CompletionPct,
    int OverdueReleases,
    int StaleItems,
    decimal Velocity3SprintAvg
);
