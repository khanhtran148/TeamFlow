using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Scheduled.Jobs;

/// <summary>
/// Processes pending and failed emails from the outbox.
/// Cron: */30 * * * * ? (every 30 seconds)
/// Retry intervals: 30s, 5m, 30m. After max_attempts: dead-letter.
/// </summary>
public sealed class EmailOutboxProcessorJob(
    ILogger<EmailOutboxProcessorJob> logger,
    TeamFlowDbContext dbContext,
    IEmailOutboxRepository emailOutboxRepository,
    IEmailSender emailSender)
    : BaseJob(logger, dbContext)
{
    private static readonly TimeSpan[] RetryIntervals =
    [
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30)
    ];

    protected override async Task ExecuteInternal(
        Quartz.IJobExecutionContext context, JobExecutionMetric metric)
    {
        var ct = context.CancellationToken;
        var pending = await emailOutboxRepository.GetPendingAsync(50, ct);

        foreach (var entry in pending)
        {
            ct.ThrowIfCancellationRequested();

            entry.Status = EmailStatus.Sending;
            entry.AttemptCount++;
            await emailOutboxRepository.UpdateAsync(entry, ct);

            try
            {
                var body = entry.BodyJson.RootElement.ToString();
                await emailSender.SendAsync(entry.RecipientEmail, entry.Subject, body, ct);

                entry.Status = EmailStatus.Sent;
                entry.SentAt = DateTime.UtcNow;
                entry.LastError = null;
                await emailOutboxRepository.UpdateAsync(entry, ct);

                metric.RecordsProcessed++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to send email {EmailId}, attempt {Attempt}/{Max}",
                    entry.Id, entry.AttemptCount, entry.MaxAttempts);

                entry.LastError = ex.Message;

                if (entry.AttemptCount >= entry.MaxAttempts)
                {
                    entry.Status = EmailStatus.DeadLettered;
                    logger.LogError(
                        "Email {EmailId} dead-lettered after {Attempts} attempts",
                        entry.Id, entry.AttemptCount);
                }
                else
                {
                    entry.Status = EmailStatus.Failed;
                    var retryIndex = Math.Min(entry.AttemptCount - 1, RetryIntervals.Length - 1);
                    entry.NextRetryAt = DateTime.UtcNow + RetryIntervals[retryIndex];
                }

                await emailOutboxRepository.UpdateAsync(entry, ct);
            }
        }
    }
}
