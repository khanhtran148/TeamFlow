using System.IO.Compression;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Scheduled.Jobs;

/// <summary>
/// Archives old domain events and hard-deletes soft-deleted work items.
/// Cron: 0 3 1 * ? (1st of month at 03:00)
/// Phase 1: DomainEvents >36 months → gzipped JSON lines export.
/// Phase 2: Soft-deleted WorkItems >30 days → hard delete (WorkItemHistories preserved).
/// Note: Human review required per CLAUDE.md rules for irreversible operations.
/// Export to local filesystem; S3 adapter deferred.
/// </summary>
public sealed class DataArchivalJob(
    ILogger<DataArchivalJob> logger,
    TeamFlowDbContext dbContext,
    IConfiguration configuration)
    : BaseJob(logger, dbContext)
{
    private string ArchivePath => Path.GetFullPath(
        configuration["Archival:BasePath"] ?? Path.Combine(AppContext.BaseDirectory, "data", "archives"));

    protected override async Task ExecuteInternal(
        Quartz.IJobExecutionContext context, JobExecutionMetric metric)
    {
        var ct = context.CancellationToken;
        var now = DateTime.UtcNow;

        // Phase 1: Archive old domain events (>36 months)
        var cutoffDate = now.AddMonths(-36);
        var oldEvents = await DbContext.DomainEvents
            .Where(e => e.OccurredAt < cutoffDate)
            .OrderBy(e => e.OccurredAt)
            .Take(10_000)
            .ToListAsync(ct);

        if (oldEvents.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            var archiveDir = Path.Combine(ArchivePath, now.ToString("yyyy-MM"));
            Directory.CreateDirectory(archiveDir);

            var archiveFile = Path.Combine(archiveDir, $"domain-events-{now:yyyyMMdd-HHmmss}.jsonl.gz");

            await using (var fileStream = File.Create(archiveFile))
            await using (var gzipStream = new GZipStream(fileStream, CompressionLevel.Optimal))
            await using (var writer = new StreamWriter(gzipStream))
            {
                foreach (var evt in oldEvents)
                {
                    var json = JsonSerializer.Serialize(new
                    {
                        evt.Id,
                        evt.EventType,
                        evt.Payload,
                        evt.OccurredAt,
                        evt.SessionId
                    });
                    await writer.WriteLineAsync(json);
                }
            }

            DbContext.DomainEvents.RemoveRange(oldEvents);
            await DbContext.SaveChangesAsync(ct);
            metric.RecordsProcessed += oldEvents.Count;

            logger.LogInformation(
                "DataArchivalJob: Archived {Count} domain events to {File}",
                oldEvents.Count, archiveFile);
        }

        // Phase 2: Hard delete soft-deleted work items (>30 days)
        var softDeleteCutoff = now.AddDays(-30);
        var softDeletedItems = await DbContext.WorkItems
            .IgnoreQueryFilters()
            .Where(w => w.DeletedAt != null && w.DeletedAt < softDeleteCutoff)
            .Take(1000)
            .ToListAsync(ct);

        if (softDeletedItems.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            DbContext.WorkItems.RemoveRange(softDeletedItems);
            await DbContext.SaveChangesAsync(ct);
            metric.RecordsProcessed += softDeletedItems.Count;

            logger.LogInformation(
                "DataArchivalJob: Hard-deleted {Count} soft-deleted work items",
                softDeletedItems.Count);
        }
    }
}
