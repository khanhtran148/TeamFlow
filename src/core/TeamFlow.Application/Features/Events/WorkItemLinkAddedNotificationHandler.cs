using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Events;

public sealed class WorkItemLinkAddedNotificationHandler(
    IBroadcastService broadcastService,
    ILogger<WorkItemLinkAddedNotificationHandler> logger)
    : INotificationHandler<WorkItemLinkAddedDomainEvent>
{
    public async Task Handle(WorkItemLinkAddedDomainEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Broadcasting WorkItemLinkAdded for {WorkItemId}", notification.WorkItemId);

        // Note: WorkItemLinkAdded doesn't have ProjectId; we use a broadcast to both items' projects
        // For now, broadcast with WorkItemId as identifier — clients filter by their subscribed items
        await broadcastService.BroadcastToUserAsync(
            notification.ActorId,
            "work_item.link_added",
            notification,
            ct);
    }
}
