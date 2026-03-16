using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Scheduled.Jobs;

/// <summary>
/// Recalculates velocity averages and trend for all projects.
/// Cron: 0 7 * * 1 (Monday 07:00)
/// </summary>
public sealed class VelocityAggregatorJob(
    ILogger<VelocityAggregatorJob> logger,
    TeamFlowDbContext dbContext)
    : BaseJob(logger, dbContext)
{
    protected override async Task ExecuteInternal(
        Quartz.IJobExecutionContext context, JobExecutionMetric metric)
    {
        var ct = context.CancellationToken;

        var projectIds = await DbContext.Projects
            .AsNoTracking()
            .Select(p => p.Id)
            .ToListAsync(ct);

        foreach (var projectId in projectIds)
        {
            ct.ThrowIfCancellationRequested();

            var velocities = await DbContext.TeamVelocityHistories
                .Where(v => v.ProjectId == projectId)
                .OrderByDescending(v => v.RecordedAt)
                .Take(6)
                .ToListAsync(ct);

            if (velocities.Count == 0) continue;

            var latest = velocities[0];
            var last3 = velocities.Take(3).Select(v => (double)v.Velocity).ToList();
            var last6 = velocities.Select(v => (double)v.Velocity).ToList();

            latest.Velocity3SprintAvg = last3.Count > 0 ? Math.Round(last3.Average(), 1) : null;
            latest.Velocity6SprintAvg = last6.Count > 0 ? Math.Round(last6.Average(), 1) : null;

            // Simple trend detection
            if (last3.Count >= 3)
            {
                var trend = last3[0] - last3[^1];
                latest.VelocityTrend = trend switch
                {
                    > 2 => "Increasing",
                    < -2 => "Decreasing",
                    _ => "Stable"
                };
            }

            metric.RecordsProcessed++;
        }

        if (metric.RecordsProcessed > 0)
            await DbContext.SaveChangesAsync(ct);

        logger.LogInformation(
            "VelocityAggregatorJob: Updated {Count} projects", metric.RecordsProcessed);
    }
}
