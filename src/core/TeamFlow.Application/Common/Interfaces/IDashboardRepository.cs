using TeamFlow.Application.Features.Dashboard.Dtos;

namespace TeamFlow.Application.Common.Interfaces;

public interface IDashboardRepository
{
    Task<VelocityChartDto> GetVelocityDataAsync(Guid projectId, int sprintCount, CancellationToken ct = default);
    Task<CumulativeFlowDto> GetCumulativeFlowDataAsync(Guid projectId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);
    Task<CycleTimeDto> GetCycleTimeDataAsync(Guid projectId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default);
    Task<WorkloadHeatmapDto> GetWorkloadDataAsync(Guid projectId, CancellationToken ct = default);
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid projectId, CancellationToken ct = default);
    Task<ReleaseProgressDto> GetReleaseProgressAsync(Guid releaseId, CancellationToken ct = default);
}
