using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Events;

public sealed class WorkItemStatusChangedNotificationHandler(
    IBroadcastService broadcastService,
    ILogger<WorkItemStatusChangedNotificationHandler> logger)
    : INotificationHandler<WorkItemStatusChangedDomainEvent>
{
    public async Task Handle(WorkItemStatusChangedDomainEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Broadcasting WorkItemStatusChanged for {WorkItemId}", notification.WorkItemId);

        await broadcastService.BroadcastToProjectAsync(
            notification.ProjectId,
            "work_item.status_changed",
            notification,
            ct);
    }
}
