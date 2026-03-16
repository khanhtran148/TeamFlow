using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Scheduled.Jobs;

/// <summary>
/// Sends deadline reminder notifications for work items with releases due in 1 or 3 days.
/// Cron: 0 8 * * ? (daily at 08:00)
/// </summary>
public sealed class DeadlineReminderJob(
    ILogger<DeadlineReminderJob> logger,
    TeamFlowDbContext dbContext,
    INotificationService notificationService)
    : BaseJob(logger, dbContext)
{
    protected override async Task ExecuteInternal(
        Quartz.IJobExecutionContext context, JobExecutionMetric metric)
    {
        var ct = context.CancellationToken;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var oneDay = today.AddDays(1);
        var threeDays = today.AddDays(3);

        var itemsDue = await DbContext.WorkItems
            .AsNoTracking()
            .Where(w => w.AssigneeId.HasValue
                && w.ReleaseId.HasValue
                && w.Status != WorkItemStatus.Done
                && w.Status != WorkItemStatus.Rejected
                && w.Release!.ReleaseDate.HasValue)
            .Where(w => w.Release!.ReleaseDate == oneDay || w.Release!.ReleaseDate == threeDays)
            .Select(w => new
            {
                w.Id,
                w.Title,
                w.AssigneeId,
                w.ProjectId,
                ReleaseDate = w.Release!.ReleaseDate!.Value,
                ReleaseName = w.Release.Name
            })
            .ToListAsync(ct);

        foreach (var item in itemsDue)
        {
            ct.ThrowIfCancellationRequested();

            var daysUntil = item.ReleaseDate.DayNumber - today.DayNumber;
            var type = daysUntil <= 1
                ? NotificationType.DeadlineReminder1d
                : NotificationType.DeadlineReminder3d;

            var title = $"Deadline reminder: {item.Title} — release \"{item.ReleaseName}\" in {daysUntil} day(s)";

            await notificationService.CreateNotificationAsync(
                item.AssigneeId!.Value,
                type,
                title,
                null,
                item.Id,
                "WorkItem",
                item.ProjectId,
                ct);

            metric.RecordsProcessed++;
        }

        logger.LogInformation(
            "DeadlineReminderJob: Sent {Count} reminders", metric.RecordsProcessed);
    }
}
