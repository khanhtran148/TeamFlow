using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Scheduled.Jobs;

[DisallowConcurrentExecution]
public sealed class EventPartitionCreatorJob : BaseJob
{
    public EventPartitionCreatorJob(
        ILogger<EventPartitionCreatorJob> logger,
        TeamFlowDbContext dbContext)
        : base(logger, dbContext)
    {
    }

    protected override async Task ExecuteInternal(IJobExecutionContext context, JobExecutionMetric metric)
    {
        await ExecuteJobAsync(context, metric);
    }

    public async Task ExecuteJobAsync(IJobExecutionContext context, JobExecutionMetric metric)
    {
        var ct = context.CancellationToken;
        var nextMonth = DateTime.UtcNow.AddMonths(1);
        var partitionName = $"domain_events_{nextMonth:yyyy_MM}";
        var rangeStart = new DateOnly(nextMonth.Year, nextMonth.Month, 1);
        var rangeEnd = rangeStart.AddMonths(1);

        Logger.LogInformation(
            "EventPartitionCreatorJob: Creating partition {PartitionName} for range {RangeStart} to {RangeEnd}",
            partitionName, rangeStart, rangeEnd);

        try
        {
            var sql = $"""
                CREATE TABLE IF NOT EXISTS {partitionName}
                PARTITION OF domain_events
                FOR VALUES FROM ('{rangeStart:yyyy-MM-dd}') TO ('{rangeEnd:yyyy-MM-dd}')
                """;

            // Only execute partition SQL on PostgreSQL (not SQLite or InMemory)
            var providerName = DbContext.Database.ProviderName ?? string.Empty;
            if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                await DbContext.Database.ExecuteSqlRawAsync(sql, ct);
            }
            else
            {
                Logger.LogInformation(
                    "EventPartitionCreatorJob: Skipping partition creation — not running on PostgreSQL (provider: {Provider})",
                    providerName);
            }

            metric.RecordsProcessed = 1;

            Logger.LogInformation(
                "EventPartitionCreatorJob: Successfully ensured partition {PartitionName} exists",
                partitionName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogCritical(
                ex,
                "EventPartitionCreatorJob: CRITICAL — Failed to create partition {PartitionName}",
                partitionName);
            throw;
        }
    }
}
