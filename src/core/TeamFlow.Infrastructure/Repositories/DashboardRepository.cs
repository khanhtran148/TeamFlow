using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.Dtos;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class DashboardRepository(TeamFlowDbContext context) : IDashboardRepository
{
    public async Task<VelocityChartDto> GetVelocityDataAsync(
        Guid projectId, int sprintCount, CancellationToken ct = default)
    {
        var velocityRecords = await context.TeamVelocityHistories
            .AsNoTracking()
            .Where(v => v.ProjectId == projectId)
            .OrderByDescending(v => v.RecordedAt)
            .Take(sprintCount)
            .Join(context.Sprints.AsNoTracking(),
                v => v.SprintId,
                s => s.Id,
                (v, s) => new { v, s })
            .OrderBy(x => x.v.RecordedAt)
            .ToListAsync(ct);

        var sprints = velocityRecords.Select(x => new VelocitySprintDto(
            x.v.SprintId,
            x.s.Name,
            x.v.PlannedPoints,
            x.v.CompletedPoints,
            x.v.Velocity,
            (decimal)(x.v.Velocity3SprintAvg ?? 0),
            (decimal)(x.v.Velocity6SprintAvg ?? 0)
        )).ToList();

        return new VelocityChartDto(sprints);
    }

    public async Task<CumulativeFlowDto> GetCumulativeFlowDataAsync(
        Guid projectId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default)
    {
        var burndownPoints = await context.BurndownDataPoints
            .AsNoTracking()
            .Where(b => b.Sprint!.ProjectId == projectId
                && b.RecordedDate >= fromDate
                && b.RecordedDate <= toDate)
            .GroupBy(b => b.RecordedDate)
            .Select(g => new CumulativeFlowPointDto(
                g.Key,
                g.Sum(b => b.RemainingPoints),
                0,
                0,
                g.Sum(b => b.CompletedPoints)
            ))
            .OrderBy(p => p.Date)
            .ToListAsync(ct);

        return new CumulativeFlowDto(burndownPoints);
    }

    public async Task<CycleTimeDto> GetCycleTimeDataAsync(
        Guid projectId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default)
    {
        // Single query: join completed transitions with start transitions
        var completedQuery = context.WorkItemHistories
            .AsNoTracking()
            .Where(h => h.WorkItem!.ProjectId == projectId
                && h.FieldName == "Status"
                && h.NewValue == nameof(WorkItemStatus.Done));

        if (fromDate.HasValue)
            completedQuery = completedQuery.Where(h => DateOnly.FromDateTime(h.CreatedAt) >= fromDate.Value);
        if (toDate.HasValue)
            completedQuery = completedQuery.Where(h => DateOnly.FromDateTime(h.CreatedAt) <= toDate.Value);

        var startQuery = context.WorkItemHistories
            .AsNoTracking()
            .Where(h => h.WorkItem!.ProjectId == projectId
                && h.FieldName == "Status"
                && h.NewValue == nameof(WorkItemStatus.InProgress))
            .GroupBy(h => h.WorkItemId)
            .Select(g => new { WorkItemId = g.Key, StartedAt = g.Min(h => h.CreatedAt) });

        var joined = await completedQuery
            .Join(startQuery,
                c => c.WorkItemId,
                s => s.WorkItemId,
                (c, s) => new
                {
                    c.WorkItem!.Type,
                    CompletedAt = c.CreatedAt,
                    s.StartedAt
                })
            .ToListAsync(ct);

        var cycleData = joined
            .GroupBy(x => x.Type.ToString())
            .Select(g =>
            {
                var days = g.Select(x => (x.CompletedAt - x.StartedAt).TotalDays).OrderBy(d => d).ToList();
                var count = days.Count;
                return new CycleTimeByTypeDto(
                    g.Key,
                    count > 0 ? Math.Round(days.Average(), 1) : 0,
                    count > 0 ? Math.Round(days[count / 2], 1) : 0,
                    count > 0 ? Math.Round(days[(int)(count * 0.9)], 1) : 0,
                    count
                );
            })
            .ToList();

        return new CycleTimeDto(cycleData);
    }

    public async Task<WorkloadHeatmapDto> GetWorkloadDataAsync(
        Guid projectId, CancellationToken ct = default)
    {
        var members = await context.WorkItems
            .AsNoTracking()
            .Where(w => w.ProjectId == projectId && w.AssigneeId.HasValue)
            .GroupBy(w => new { w.AssigneeId, w.Assignee!.Name })
            .Select(g => new WorkloadMemberDto(
                g.Key.AssigneeId!.Value,
                g.Key.Name,
                g.Count(),
                g.Count(w => w.Status == WorkItemStatus.InProgress),
                g.Sum(w => w.EstimationValue ?? 0)
            ))
            .OrderByDescending(m => m.AssignedCount)
            .ToListAsync(ct);

        return new WorkloadHeatmapDto(members);
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        Guid projectId, CancellationToken ct = default)
    {
        var staleThreshold = DateTime.UtcNow.AddDays(-14);

        // Run all independent queries in parallel
        var activeSprintTask = context.Sprints
            .AsNoTracking()
            .Where(s => s.ProjectId == projectId && s.Status == SprintStatus.Active)
            .Select(s => new { s.Id, s.Name })
            .FirstOrDefaultAsync(ct);

        var totalItemsTask = context.WorkItems
            .AsNoTracking()
            .CountAsync(w => w.ProjectId == projectId, ct);

        var openItemsTask = context.WorkItems
            .AsNoTracking()
            .CountAsync(w => w.ProjectId == projectId
                && w.Status != WorkItemStatus.Done
                && w.Status != WorkItemStatus.Rejected, ct);

        var overdueReleasesTask = context.Releases
            .AsNoTracking()
            .CountAsync(r => r.ProjectId == projectId && r.Status == ReleaseStatus.Overdue, ct);

        var staleItemsTask = context.WorkItems
            .AsNoTracking()
            .CountAsync(w => w.ProjectId == projectId
                && w.Status == WorkItemStatus.InProgress
                && w.UpdatedAt < staleThreshold, ct);

        var velocity3AvgTask = context.TeamVelocityHistories
            .AsNoTracking()
            .Where(v => v.ProjectId == projectId)
            .OrderByDescending(v => v.RecordedAt)
            .Take(1)
            .Select(v => v.Velocity3SprintAvg)
            .FirstOrDefaultAsync(ct);

        await Task.WhenAll(activeSprintTask, totalItemsTask, openItemsTask,
            overdueReleasesTask, staleItemsTask, velocity3AvgTask);

        var activeSprint = await activeSprintTask;
        var totalItems = await totalItemsTask;
        var openItems = await openItemsTask;
        var completionPct = totalItems > 0 ? Math.Round((double)(totalItems - openItems) / totalItems, 3) : 0;

        return new DashboardSummaryDto(
            activeSprint?.Id,
            activeSprint?.Name,
            totalItems,
            openItems,
            completionPct,
            await overdueReleasesTask,
            await staleItemsTask,
            (decimal)(await velocity3AvgTask ?? 0)
        );
    }

    public async Task<ReleaseProgressDto> GetReleaseProgressAsync(
        Guid releaseId, CancellationToken ct = default)
    {
        var aggregated = await context.WorkItems
            .AsNoTracking()
            .Where(w => w.ReleaseId == releaseId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                DoneCount = g.Count(w => w.Status == WorkItemStatus.Done),
                InProgressCount = g.Count(w =>
                    w.Status == WorkItemStatus.InProgress || w.Status == WorkItemStatus.InReview),
                TodoCount = g.Count(w =>
                    w.Status == WorkItemStatus.ToDo || w.Status == WorkItemStatus.NeedsClarification),
                DonePoints = g.Where(w => w.Status == WorkItemStatus.Done)
                    .Sum(w => w.EstimationValue ?? 0),
                TotalPoints = g.Sum(w => w.EstimationValue ?? 0)
            })
            .FirstOrDefaultAsync(ct);

        if (aggregated is null)
            return new ReleaseProgressDto(0, 0, 0, 0, 0, 0);

        var completionPct = aggregated.TotalPoints > 0
            ? Math.Round((double)aggregated.DonePoints / (double)aggregated.TotalPoints, 3)
            : 0;

        return new ReleaseProgressDto(
            aggregated.DoneCount, aggregated.InProgressCount, aggregated.TodoCount,
            aggregated.DonePoints, aggregated.TotalPoints, completionPct);
    }
}
