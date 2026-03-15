using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Scheduled.Jobs;

[DisallowConcurrentExecution]
public sealed class ReleaseOverdueDetectorJob : BaseJob
{
    private readonly IBroadcastService _broadcastService;
    private readonly IPublisher _publisher;

    public ReleaseOverdueDetectorJob(
        ILogger<ReleaseOverdueDetectorJob> logger,
        TeamFlowDbContext dbContext,
        IBroadcastService broadcastService,
        IPublisher publisher)
        : base(logger, dbContext)
    {
        _broadcastService = broadcastService;
        _publisher = publisher;
    }

    protected override async Task ExecuteInternal(IJobExecutionContext context, JobExecutionMetric metric)
    {
        await ExecuteJobAsync(context, metric);
    }

    public async Task ExecuteJobAsync(IJobExecutionContext context, JobExecutionMetric metric)
    {
        var ct = context.CancellationToken;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var overdueReleases = await DbContext.Releases
            .Include(r => r.WorkItems)
            .Where(r => r.Status == ReleaseStatus.Unreleased
                        && r.ReleaseDate != null
                        && r.ReleaseDate < today)
            .ToListAsync(ct);

        if (overdueReleases.Count == 0)
        {
            Logger.LogInformation("ReleaseOverdueDetectorJob: No overdue releases found");
            return;
        }

        foreach (var release in overdueReleases)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                release.Status = ReleaseStatus.Overdue;
                await DbContext.SaveChangesAsync(ct);

                var incompleteCount = release.WorkItems
                    .Count(w => w.Status is not WorkItemStatus.Done and not WorkItemStatus.Rejected);

                await _publisher.Publish(
                    new ReleaseOverdueDetectedDomainEvent(
                        release.Id,
                        release.ProjectId,
                        release.Name,
                        release.ReleaseDate!.Value,
                        incompleteCount),
                    ct);

                await _broadcastService.BroadcastToProjectAsync(
                    release.ProjectId,
                    "release.overdue_detected",
                    new { releaseId = release.Id, projectId = release.ProjectId, releaseName = release.Name, releaseDate = release.ReleaseDate },
                    ct);

                metric.RecordsProcessed++;

                Logger.LogInformation(
                    "ReleaseOverdueDetectorJob: Release {ReleaseId} ({ReleaseName}) marked overdue — {IncompleteCount} incomplete items",
                    release.Id, release.Name, incompleteCount);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                metric.RecordsFailed++;
                Logger.LogError(ex, "ReleaseOverdueDetectorJob: Failed processing release {ReleaseId}", release.Id);
            }
        }
    }
}
