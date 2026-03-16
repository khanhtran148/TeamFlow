namespace TeamFlow.Application.Features.Reports;

public sealed record SprintReportDto(
    Guid Id,
    Guid SprintId,
    Guid ProjectId,
    object ReportData,
    DateTime GeneratedAt,
    string GeneratedBy
);

public sealed record TeamHealthSummaryDto(
    Guid Id,
    Guid ProjectId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    object SummaryData,
    DateTime GeneratedAt
);
