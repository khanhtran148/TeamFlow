using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Events;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.BackgroundServices.EventDriven.Consumers;

namespace TeamFlow.BackgroundServices.Consumers;

/// <summary>
/// Persists every domain event to the domain_events table for AI event log and auditability.
/// </summary>
public sealed class DomainEventStoreConsumer(
    ILogger<DomainEventStoreConsumer> logger,
    TeamFlowDbContext dbContext,
    IBroadcastService broadcastService)
    : BaseConsumer<WorkItemCreatedDomainEvent>(logger, dbContext, broadcastService)
{
    protected override async Task ConsumeInternal(ConsumeContext<WorkItemCreatedDomainEvent> context)
    {
        var @event = context.Message;

        var domainEvent = new DomainEvent
        {
            EventType = nameof(WorkItemCreatedDomainEvent),
            AggregateType = "WorkItem",
            AggregateId = @event.WorkItemId,
            ActorId = @event.ActorId,
            OccurredAt = DateTime.UtcNow,
            Payload = JsonDocument.Parse(JsonSerializer.Serialize(@event))
        };

        DbContext.DomainEvents.Add(domainEvent);
        await DbContext.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Stored DomainEvent {EventType} for {EntityId}", domainEvent.EventType, domainEvent.AggregateId);
    }
}
