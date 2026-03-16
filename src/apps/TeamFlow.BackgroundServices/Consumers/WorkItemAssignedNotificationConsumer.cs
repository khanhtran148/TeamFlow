using MassTransit;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.BackgroundServices.EventDriven.Consumers;
using TeamFlow.Domain.Events;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.BackgroundServices.Consumers;

public sealed class WorkItemAssignedNotificationConsumer(
    ILogger<WorkItemAssignedNotificationConsumer> logger,
    TeamFlowDbContext dbContext,
    IBroadcastService broadcastService,
    INotificationService notificationService,
    IWorkItemRepository workItemRepository)
    : BaseConsumer<WorkItemAssignedDomainEvent>(logger, dbContext, broadcastService)
{
    protected override async Task ConsumeInternal(ConsumeContext<WorkItemAssignedDomainEvent> context)
    {
        var @event = context.Message;
        var ct = context.CancellationToken;

        var workItem = await workItemRepository.GetByIdAsync(@event.WorkItemId, ct);
        var title = workItem is not null
            ? $"You were assigned to {workItem.Title}"
            : "You were assigned to a work item";

        await notificationService.CreateNotificationAsync(
            @event.NewAssigneeId,
            "WorkItemAssigned",
            title,
            workItem?.Description,
            @event.WorkItemId,
            "WorkItem",
            @event.ProjectId,
            ct);

        logger.LogInformation(
            "WorkItemAssignedNotificationConsumer: Notification created for user {UserId} on item {WorkItemId}",
            @event.NewAssigneeId, @event.WorkItemId);
    }
}
