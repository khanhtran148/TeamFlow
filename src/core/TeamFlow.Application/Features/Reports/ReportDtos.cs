using System.Text.Json;

namespace TeamFlow.Application.Features.Reports;

public sealed record SprintReportDto(
    Guid Id,
    Guid SprintId,
    Guid ProjectId,
    JsonDocument ReportData,
    DateTime GeneratedAt,
    string GeneratedBy
);

public sealed record TeamHealthSummaryDto(
    Guid Id,
    Guid ProjectId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    JsonDocument SummaryData,
    DateTime GeneratedAt
);
