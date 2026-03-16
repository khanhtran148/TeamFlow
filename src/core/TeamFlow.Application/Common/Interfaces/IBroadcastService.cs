namespace TeamFlow.Application.Common.Interfaces;

public interface IBroadcastService
{
    /// <summary>
    /// Broadcasts an event to all clients subscribed to the given project group.
    /// </summary>
    Task BroadcastToProjectAsync(Guid projectId, string eventName, object payload, CancellationToken ct = default);

    /// <summary>
    /// Broadcasts an event to all clients subscribed to the given sprint group.
    /// </summary>
    Task BroadcastToSprintAsync(Guid sprintId, string eventName, object payload, CancellationToken ct = default);

    /// <summary>
    /// Broadcasts an event to a specific user.
    /// </summary>
    Task BroadcastToUserAsync(Guid userId, string eventName, object payload, CancellationToken ct = default);

    /// <summary>
    /// Broadcasts to all clients subscribed to a retro session.
    /// </summary>
    Task BroadcastToRetroSessionAsync(Guid sessionId, string eventName, object payload, CancellationToken ct = default);

    /// <summary>
    /// Broadcasts an event to all clients subscribed to the given work item group.
    /// </summary>
    Task BroadcastToWorkItemAsync(Guid workItemId, string eventName, object payload, CancellationToken ct = default);
}
