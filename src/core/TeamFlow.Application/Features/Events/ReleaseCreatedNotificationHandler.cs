using MediatR;
using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Events;

public sealed class ReleaseCreatedNotificationHandler(
    IBroadcastService broadcastService,
    ILogger<ReleaseCreatedNotificationHandler> logger)
    : INotificationHandler<ReleaseCreatedDomainEvent>
{
    public async Task Handle(ReleaseCreatedDomainEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Broadcasting ReleaseCreated for {ReleaseId} in project {ProjectId}",
            notification.ReleaseId, notification.ProjectId);

        await broadcastService.BroadcastToProjectAsync(
            notification.ProjectId,
            "release.created",
            notification,
            ct);
    }
}
