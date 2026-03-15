using MassTransit;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Events;
using TeamFlow.Infrastructure.Persistence;
using TeamFlow.BackgroundServices.EventDriven.Consumers;

namespace TeamFlow.BackgroundServices.Consumers;

/// <summary>
/// Consumes domain events and broadcasts them to SignalR clients in the appropriate project group.
/// </summary>
public sealed class SignalRBroadcastConsumer(
    ILogger<SignalRBroadcastConsumer> logger,
    TeamFlowDbContext dbContext,
    IBroadcastService broadcastService)
    : BaseConsumer<WorkItemCreatedDomainEvent>(logger, dbContext, broadcastService)
{
    protected override async Task ConsumeInternal(ConsumeContext<WorkItemCreatedDomainEvent> context)
    {
        var @event = context.Message;

        await BroadcastService.BroadcastToProjectAsync(
            @event.ProjectId,
            "work_item.created",
            @event,
            context.CancellationToken);
    }
}

/// <summary>
/// Broadcasts work item status changes to project groups.
/// </summary>
public sealed class SignalRWorkItemStatusBroadcastConsumer(
    ILogger<SignalRWorkItemStatusBroadcastConsumer> logger,
    TeamFlowDbContext dbContext,
    IBroadcastService broadcastService)
    : BaseConsumer<WorkItemStatusChangedDomainEvent>(logger, dbContext, broadcastService)
{
    protected override async Task ConsumeInternal(ConsumeContext<WorkItemStatusChangedDomainEvent> context)
    {
        var @event = context.Message;

        await BroadcastService.BroadcastToProjectAsync(
            @event.ProjectId,
            "work_item.status_changed",
            @event,
            context.CancellationToken);
    }
}
