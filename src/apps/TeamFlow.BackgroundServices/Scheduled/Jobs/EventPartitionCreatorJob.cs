using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
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

        // DDL must run outside any ambient EF transaction because:
        // 1. PostgreSQL DDL statements cannot be rolled back once committed.
        // 2. A failed DDL inside a transaction poisons the whole transaction.
        // We open a dedicated connection so the DDL is isolated.
        var providerName = DbContext.Database.ProviderName ?? string.Empty;
        if (!providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogInformation(
                "EventPartitionCreatorJob: Skipping partition creation — not running on PostgreSQL (provider: {Provider})",
                providerName);
            metric.RecordsProcessed = 1;
            return;
        }

        var connectionString = DbContext.Database.GetConnectionString()!;
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var sql = $"""
            CREATE TABLE IF NOT EXISTS {partitionName}
            PARTITION OF domain_events
            FOR VALUES FROM ('{rangeStart:yyyy-MM-dd}') TO ('{rangeEnd:yyyy-MM-dd}')
            """;

        try
        {
            await using var cmd = new NpgsqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync(ct);

            metric.RecordsProcessed = 1;

            Logger.LogInformation(
                "EventPartitionCreatorJob: Successfully ensured partition {PartitionName} exists",
                partitionName);
        }
        catch (PostgresException pgEx) when (pgEx.SqlState == "42P17")
        {
            // 42P17: "domain_events" is not partitioned.
            // This occurs in environments where the table was not set up with declarative
            // partitioning (e.g., local dev or test databases created via EnsureCreated).
            // Log a warning and continue — the job is a no-op in this case.
            Logger.LogWarning(
                pgEx,
                "EventPartitionCreatorJob: Skipping partition creation — domain_events is not a partitioned table in this environment");
            metric.RecordsProcessed = 1;
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
