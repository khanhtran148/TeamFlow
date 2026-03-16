using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Scheduled.Jobs;

/// <summary>
/// Generates a sprint report after sprint completion.
/// Event-triggered: SprintCompletedConsumer enqueues this job.
/// </summary>
public sealed class SprintReportGeneratorJob(
    ILogger<SprintReportGeneratorJob> logger,
    TeamFlowDbContext dbContext,
    ISprintReportRepository sprintReportRepository,
    IBroadcastService broadcastService)
    : BaseJob(logger, dbContext)
{
    protected override async Task ExecuteInternal(
        Quartz.IJobExecutionContext context, JobExecutionMetric metric)
    {
        var ct = context.CancellationToken;

        // Find completed sprints without a report
        var sprintsWithoutReport = await DbContext.Sprints
            .AsNoTracking()
            .Where(s => s.Status == SprintStatus.Completed)
            .Where(s => !DbContext.SprintReports.Any(r => r.SprintId == s.Id))
            .Select(s => new { s.Id, s.ProjectId, s.Name })
            .ToListAsync(ct);

        foreach (var sprint in sprintsWithoutReport)
        {
            ct.ThrowIfCancellationRequested();

            var velocity = await DbContext.TeamVelocityHistories
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.SprintId == sprint.Id, ct);

            var snapshot = await DbContext.SprintSnapshots
                .AsNoTracking()
                .Where(s => s.SprintId == sprint.Id && s.IsFinal)
                .FirstOrDefaultAsync(ct);

            var workItemCount = await DbContext.WorkItems
                .AsNoTracking()
                .CountAsync(w => w.SprintId == sprint.Id, ct);

            var doneCount = await DbContext.WorkItems
                .AsNoTracking()
                .CountAsync(w => w.SprintId == sprint.Id && w.Status == WorkItemStatus.Done, ct);

            var reportData = new
            {
                sprintName = sprint.Name,
                plannedPoints = velocity?.PlannedPoints ?? 0,
                completedPoints = velocity?.CompletedPoints ?? 0,
                velocity = velocity?.Velocity ?? 0,
                totalItems = workItemCount,
                completedItems = doneCount,
                predictabilityScore = velocity is not null && velocity.PlannedPoints > 0
                    ? Math.Round((double)velocity.CompletedPoints / velocity.PlannedPoints * 100, 1)
                    : 0,
                snapshotExists = snapshot is not null
            };

            var report = new SprintReport
            {
                SprintId = sprint.Id,
                ProjectId = sprint.ProjectId,
                ReportData = JsonSerializer.SerializeToDocument(reportData),
                GeneratedBy = "System"
            };

            await sprintReportRepository.AddAsync(report, ct);

            await broadcastService.BroadcastToProjectAsync(
                sprint.ProjectId,
                "sprint.report_ready",
                new { SprintId = sprint.Id, sprint.Name, ReportId = report.Id },
                ct);

            metric.RecordsProcessed++;
        }

        logger.LogInformation(
            "SprintReportGeneratorJob: Generated {Count} reports", metric.RecordsProcessed);
    }
}
