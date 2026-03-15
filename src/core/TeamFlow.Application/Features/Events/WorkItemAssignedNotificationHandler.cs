using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Events;

public sealed class WorkItemAssignedNotificationHandler(
    IBroadcastService broadcastService,
    ILogger<WorkItemAssignedNotificationHandler> logger)
    : INotificationHandler<WorkItemAssignedDomainEvent>
{
    public async Task Handle(WorkItemAssignedDomainEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Broadcasting WorkItemAssigned for {WorkItemId}", notification.WorkItemId);

        await broadcastService.BroadcastToProjectAsync(
            notification.ProjectId,
            "work_item.assigned",
            notification,
            ct);
    }
}
