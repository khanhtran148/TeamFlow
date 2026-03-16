using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Scheduled.Jobs;

/// <summary>
/// Aggregates weekly metrics per project and stores as team health summary.
/// Cron: 0 7 * * 1 (Monday 07:00, runs after VelocityAggregatorJob)
/// </summary>
public sealed class TeamHealthSummaryJob(
    ILogger<TeamHealthSummaryJob> logger,
    TeamFlowDbContext dbContext,
    ITeamHealthSummaryRepository teamHealthRepository)
    : BaseJob(logger, dbContext)
{
    protected override async Task ExecuteInternal(
        Quartz.IJobExecutionContext context, JobExecutionMetric metric)
    {
        var ct = context.CancellationToken;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekStart = today.AddDays(-(int)today.DayOfWeek + 1); // Monday
        var weekEnd = weekStart.AddDays(6); // Sunday

        var projectIds = await DbContext.Projects
            .AsNoTracking()
            .Select(p => p.Id)
            .ToListAsync(ct);

        foreach (var projectId in projectIds)
        {
            ct.ThrowIfCancellationRequested();

            // Check if already generated for this period
            var existing = await DbContext.Set<TeamHealthSummary>()
                .AsNoTracking()
                .AnyAsync(s => s.ProjectId == projectId && s.PeriodStart == weekStart, ct);

            if (existing) continue;

            // Gather metrics
            var totalItems = await DbContext.WorkItems
                .AsNoTracking()
                .CountAsync(w => w.ProjectId == projectId, ct);

            var bugCount = await DbContext.WorkItems
                .AsNoTracking()
                .CountAsync(w => w.ProjectId == projectId
                    && w.Type == WorkItemType.Bug
                    && w.CreatedAt >= weekStart.ToDateTime(TimeOnly.MinValue)
                    && w.CreatedAt <= weekEnd.ToDateTime(TimeOnly.MaxValue), ct);

            var staleCount = await DbContext.WorkItems
                .AsNoTracking()
                .CountAsync(w => w.ProjectId == projectId
                    && w.Status == WorkItemStatus.InProgress
                    && w.UpdatedAt < DateTime.UtcNow.AddDays(-14), ct);

            var latestVelocity = await DbContext.TeamVelocityHistories
                .AsNoTracking()
                .Where(v => v.ProjectId == projectId)
                .OrderByDescending(v => v.RecordedAt)
                .FirstOrDefaultAsync(ct);

            var summaryData = new
            {
                totalItems,
                newBugsThisWeek = bugCount,
                staleItems = staleCount,
                bugRate = totalItems > 0 ? Math.Round((double)bugCount / totalItems * 100, 1) : 0,
                velocity3SprintAvg = latestVelocity?.Velocity3SprintAvg ?? 0,
                velocityTrend = latestVelocity?.VelocityTrend ?? "Unknown"
            };

            var summary = new TeamHealthSummary
            {
                ProjectId = projectId,
                PeriodStart = weekStart,
                PeriodEnd = weekEnd,
                SummaryData = JsonSerializer.SerializeToDocument(summaryData)
            };

            await teamHealthRepository.AddAsync(summary, ct);
            metric.RecordsProcessed++;
        }

        logger.LogInformation(
            "TeamHealthSummaryJob: Generated {Count} summaries", metric.RecordsProcessed);
    }
}
