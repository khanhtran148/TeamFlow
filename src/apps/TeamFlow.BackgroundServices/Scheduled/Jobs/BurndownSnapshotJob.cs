using Microsoft.EntityFrameworkCore;
using Quartz;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Scheduled.Jobs;

[DisallowConcurrentExecution]
public sealed class BurndownSnapshotJob(
    ILogger<BurndownSnapshotJob> logger,
    TeamFlowDbContext dbContext,
    IBroadcastService broadcastService)
    : BaseJob(logger, dbContext)
{

    protected override async Task ExecuteInternal(IJobExecutionContext context, JobExecutionMetric metric)
    {
        await ExecuteJobAsync(context, metric);
    }

    public async Task ExecuteJobAsync(IJobExecutionContext context, JobExecutionMetric metric)
    {
        var ct = context.CancellationToken;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var activeSprints = await DbContext.Sprints
            .Include(s => s.WorkItems)
            .Where(s => s.Status == SprintStatus.Active)
            .ToListAsync(ct);

        if (activeSprints.Count == 0)
        {
            Logger.LogInformation("BurndownSnapshotJob: No active sprints found");
            return;
        }

        foreach (var sprint in activeSprints)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var alreadyExists = await DbContext.BurndownDataPoints
                    .AnyAsync(b => b.SprintId == sprint.Id && b.RecordedDate == today, ct);

                if (alreadyExists)
                {
                    Logger.LogInformation(
                        "BurndownSnapshotJob: Skipping sprint {SprintId} — data point already exists for {Date}",
                        sprint.Id, today);
                    continue;
                }

                var completedPoints = sprint.WorkItems
                    .Where(w => w.Status is WorkItemStatus.Done or WorkItemStatus.Rejected)
                    .Sum(w => (int)(w.EstimationValue ?? 0));

                var remainingPoints = sprint.WorkItems
                    .Where(w => w.Status is not WorkItemStatus.Done and not WorkItemStatus.Rejected)
                    .Sum(w => (int)(w.EstimationValue ?? 0));

                var totalPoints = completedPoints + remainingPoints;

                var dataPoint = new BurndownDataPoint
                {
                    SprintId = sprint.Id,
                    RecordedDate = today,
                    RemainingPoints = remainingPoints,
                    CompletedPoints = completedPoints,
                    AddedPoints = 0,
                    IsWeekend = today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                };

                DbContext.BurndownDataPoints.Add(dataPoint);
                await DbContext.SaveChangesAsync(ct);

                // Check "At Risk" condition: remaining > ideal * 1.2
                if (sprint.StartDate is not null && sprint.EndDate is not null && totalPoints > 0)
                {
                    var totalDays = sprint.EndDate.Value.DayNumber - sprint.StartDate.Value.DayNumber;
                    var daysElapsed = today.DayNumber - sprint.StartDate.Value.DayNumber;

                    if (totalDays > 0)
                    {
                        var idealRemaining = totalPoints * (1.0 - (double)daysElapsed / totalDays);
                        if (idealRemaining < 0) idealRemaining = 0;

                        if (remainingPoints > idealRemaining * 1.2)
                        {
                            Logger.LogWarning(
                                "BurndownSnapshotJob: Sprint {SprintId} is At Risk — remaining {Remaining} > ideal {Ideal} * 1.2",
                                sprint.Id, remainingPoints, idealRemaining);
                        }
                    }
                }

                await broadcastService.BroadcastToSprintAsync(
                    sprint.Id,
                    "burndown.updated",
                    new { sprintId = sprint.Id, recordedDate = today, remainingPoints, completedPoints },
                    ct);

                metric.RecordsProcessed++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                metric.RecordsFailed++;
                Logger.LogError(ex, "BurndownSnapshotJob: Failed processing sprint {SprintId}", sprint.Id);
            }
        }
    }
}
