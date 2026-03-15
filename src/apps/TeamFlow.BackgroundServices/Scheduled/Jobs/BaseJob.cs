using Microsoft.Extensions.Logging;
using Quartz;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Scheduled.Jobs;

/// <summary>
/// Base class for all scheduled Quartz.NET jobs.
/// Implements checkpoint pattern and metrics recording.
/// </summary>
public abstract class BaseJob : IJob
{
    protected readonly ILogger Logger;
    protected readonly TeamFlowDbContext DbContext;

    protected BaseJob(ILogger logger, TeamFlowDbContext dbContext)
    {
        Logger = logger;
        DbContext = dbContext;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var jobType = GetType().Name;
        var jobRunId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;

        var metric = new JobExecutionMetric
        {
            JobType = jobType,
            JobRunId = jobRunId,
            Status = "Running",
            StartedAt = startedAt
        };

        DbContext.JobExecutionMetrics.Add(metric);
        await DbContext.SaveChangesAsync(context.CancellationToken);

        Logger.LogInformation("Job {JobType} started | RunId: {JobRunId}", jobType, jobRunId);

        try
        {
            await ExecuteInternal(context, metric);

            metric.Status = "Success";
            metric.CompletedAt = DateTime.UtcNow;
            metric.DurationMs = (int)(DateTime.UtcNow - startedAt).TotalMilliseconds;
            await DbContext.SaveChangesAsync(context.CancellationToken);

            Logger.LogInformation(
                "Job {JobType} completed | RunId: {JobRunId} | Duration: {DurationMs}ms | Processed: {Records}",
                jobType, jobRunId, metric.DurationMs, metric.RecordsProcessed);
        }
        catch (OperationCanceledException)
        {
            metric.Status = "Cancelled";
            metric.CompletedAt = DateTime.UtcNow;
            metric.DurationMs = (int)(DateTime.UtcNow - startedAt).TotalMilliseconds;
            await DbContext.SaveChangesAsync(CancellationToken.None);

            Logger.LogWarning("Job {JobType} cancelled | RunId: {JobRunId}", jobType, jobRunId);
            throw;
        }
        catch (Exception ex)
        {
            metric.Status = "Failed";
            metric.CompletedAt = DateTime.UtcNow;
            metric.DurationMs = (int)(DateTime.UtcNow - startedAt).TotalMilliseconds;
            metric.ErrorMessage = ex.Message;
            await DbContext.SaveChangesAsync(CancellationToken.None);

            Logger.LogError(ex, "Job {JobType} failed | RunId: {JobRunId}", jobType, jobRunId);
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }

    /// <summary>Override to implement job-specific logic. Use checkpoint pattern with cancellation.</summary>
    protected abstract Task ExecuteInternal(IJobExecutionContext context, JobExecutionMetric metric);
}
