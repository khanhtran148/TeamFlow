using System.Text.Json;
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
public sealed class StaleItemDetectorJob : BaseJob
{
    private const int StaleDaysThreshold = 14;

    private readonly IBroadcastService _broadcastService;
    private readonly IPublisher _publisher;

    public StaleItemDetectorJob(
        ILogger<StaleItemDetectorJob> logger,
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
        var staleThreshold = DateTime.UtcNow.AddDays(-StaleDaysThreshold);

        // Query stale items: not done/rejected, not deleted, not updated in 14 days, in active projects
        var staleItems = await DbContext.WorkItems
            .Include(w => w.Project)
            .Where(w => w.DeletedAt == null
                        && w.Status != WorkItemStatus.Done
                        && w.Status != WorkItemStatus.Rejected
                        && w.UpdatedAt < staleThreshold)
            .ToListAsync(ct);

        // Filter out items in archived projects (in-memory since Project.Status is string)
        var activeStaleItems = staleItems
            .Where(w => w.Project is not null && w.Project.Status != "Archived")
            .ToList();

        if (activeStaleItems.Count == 0)
        {
            Logger.LogInformation("StaleItemDetectorJob: No stale items found");
            return;
        }

        // Get active sprint IDs for severity calculation
        var activeSprintIds = await DbContext.Sprints
            .Where(s => s.Status == SprintStatus.Active)
            .Select(s => s.Id)
            .ToHashSetAsync(ct);

        foreach (var item in activeStaleItems)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var daysSinceUpdate = (int)(DateTime.UtcNow - item.UpdatedAt).TotalDays;
                var severity = CalculateSeverity(item, activeSprintIds);

                // Update ai_metadata stale_flag
                var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                    item.AiMetadata.RootElement.GetRawText()) ?? [];
                metadata["stale_flag"] = JsonSerializer.SerializeToElement(true);
                item.AiMetadata = JsonDocument.Parse(JsonSerializer.Serialize(metadata));

                await DbContext.SaveChangesAsync(ct);

                await _publisher.Publish(
                    new WorkItemStaleFlaggedDomainEvent(
                        item.Id,
                        item.ProjectId,
                        severity,
                        daysSinceUpdate),
                    ct);

                metric.RecordsProcessed++;

                Logger.LogInformation(
                    "StaleItemDetectorJob: Flagged work item {WorkItemId} as stale — severity {Severity}, {DaysSinceUpdate} days since update",
                    item.Id, severity, daysSinceUpdate);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                metric.RecordsFailed++;
                Logger.LogError(ex, "StaleItemDetectorJob: Failed processing work item {WorkItemId}", item.Id);
            }
        }
    }

    private static string CalculateSeverity(WorkItem item, HashSet<Guid> activeSprintIds)
    {
        // Critical: item is in an active sprint
        if (item.SprintId is not null && activeSprintIds.Contains(item.SprintId.Value))
            return "Critical";

        // High: item is assigned to a release
        if (item.ReleaseId is not null)
            return "High";

        // Medium: item is assigned to someone
        if (item.AssigneeId is not null)
            return "Medium";

        // Low: unassigned, not in sprint or release
        return "Low";
    }
}
