using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Events;

public sealed class WorkItemCreatedNotificationHandler(
    IBroadcastService broadcastService,
    ILogger<WorkItemCreatedNotificationHandler> logger)
    : INotificationHandler<WorkItemCreatedDomainEvent>
{
    public async Task Handle(WorkItemCreatedDomainEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Broadcasting WorkItemCreated for {WorkItemId} in project {ProjectId}",
            notification.WorkItemId, notification.ProjectId);

        await broadcastService.BroadcastToProjectAsync(
            notification.ProjectId,
            "work_item.created",
            notification,
            ct);
    }
}
