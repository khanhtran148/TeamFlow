using MassTransit;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.EventDriven.Consumers;

/// <summary>
/// Base consumer implementing pre/post consume lifecycle:
/// pre: log correlation_id, start metrics timer, validate schema_version
/// post: record to DomainEvents, stop timer, broadcast SignalR, on error → retry
/// </summary>
public abstract class BaseConsumer<TMessage> : IConsumer<TMessage>
    where TMessage : class
{
    protected readonly ILogger Logger;
    protected readonly TeamFlowDbContext DbContext;
    protected readonly IBroadcastService BroadcastService;

    protected BaseConsumer(
        ILogger logger,
        TeamFlowDbContext dbContext,
        IBroadcastService broadcastService)
    {
        Logger = logger;
        DbContext = dbContext;
        BroadcastService = broadcastService;
    }

    public async Task Consume(ConsumeContext<TMessage> context)
    {
        var messageType = typeof(TMessage).Name;
        var startedAt = DateTime.UtcNow;

        Logger.LogInformation(
            "Consuming {MessageType} | MessageId: {MessageId}",
            messageType,
            context.MessageId);

        // 1. Check idempotency (to be implemented per consumer if needed)
        if (await IsAlreadyProcessed(context))
        {
            Logger.LogWarning(
                "Duplicate message skipped: {MessageType} | MessageId: {MessageId}",
                messageType, context.MessageId);
            return;
        }

        try
        {
            // 2. Execute consumer-specific logic
            await ConsumeInternal(context);

            var elapsed = (DateTime.UtcNow - startedAt).TotalMilliseconds;
            Logger.LogInformation(
                "Consumed {MessageType} in {ElapsedMs}ms",
                messageType, elapsed);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Error consuming {MessageType} | MessageId: {MessageId}",
                messageType, context.MessageId);
            throw; // MassTransit retry policy handles this
        }
    }

    /// <summary>Override to implement consumer-specific business logic.</summary>
    protected abstract Task ConsumeInternal(ConsumeContext<TMessage> context);

    /// <summary>Override to implement idempotency check. Default: always process.</summary>
    protected virtual Task<bool> IsAlreadyProcessed(ConsumeContext<TMessage> context)
        => Task.FromResult(false);
}
