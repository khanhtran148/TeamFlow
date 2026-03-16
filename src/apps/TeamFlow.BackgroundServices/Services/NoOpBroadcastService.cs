using Microsoft.Extensions.Logging;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.BackgroundServices.Services;

/// <summary>
/// No-op IBroadcastService for background services where SignalR hub context is unavailable.
/// In production, this should be replaced with a backplane-based implementation (e.g., Redis).
/// </summary>
public sealed class NoOpBroadcastService(ILogger<NoOpBroadcastService> logger) : IBroadcastService
{
    public Task BroadcastToProjectAsync(Guid projectId, string eventName, object payload, CancellationToken ct = default)
    {
        logger.LogDebug("Broadcast to project {ProjectId}: {EventName} (no-op in background service)", projectId, eventName);
        return Task.CompletedTask;
    }

    public Task BroadcastToSprintAsync(Guid sprintId, string eventName, object payload, CancellationToken ct = default)
    {
        logger.LogDebug("Broadcast to sprint {SprintId}: {EventName} (no-op in background service)", sprintId, eventName);
        return Task.CompletedTask;
    }

    public Task BroadcastToUserAsync(Guid userId, string eventName, object payload, CancellationToken ct = default)
    {
        logger.LogDebug("Broadcast to user {UserId}: {EventName} (no-op in background service)", userId, eventName);
        return Task.CompletedTask;
    }

    public Task BroadcastToRetroSessionAsync(Guid sessionId, string eventName, object payload, CancellationToken ct = default)
    {
        logger.LogDebug("Broadcast to retro {SessionId}: {EventName} (no-op in background service)", sessionId, eventName);
        return Task.CompletedTask;
    }

    public Task BroadcastToWorkItemAsync(Guid workItemId, string eventName, object payload, CancellationToken ct = default)
    {
        logger.LogDebug("Broadcast to work item {WorkItemId}: {EventName} (no-op in background service)", workItemId, eventName);
        return Task.CompletedTask;
    }
}
