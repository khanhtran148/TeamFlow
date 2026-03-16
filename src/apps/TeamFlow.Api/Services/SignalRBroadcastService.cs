using Microsoft.AspNetCore.SignalR;
using TeamFlow.Api.Hubs;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Api.Services;

public sealed class SignalRBroadcastService(IHubContext<TeamFlowHub> hubContext) : IBroadcastService
{
    public async Task BroadcastToProjectAsync(Guid projectId, string eventName, object payload, CancellationToken ct = default)
        => await hubContext.Clients.Group($"project:{projectId}").SendAsync(eventName, payload, ct);

    public async Task BroadcastToSprintAsync(Guid sprintId, string eventName, object payload, CancellationToken ct = default)
        => await hubContext.Clients.Group($"sprint:{sprintId}").SendAsync(eventName, payload, ct);

    public async Task BroadcastToUserAsync(Guid userId, string eventName, object payload, CancellationToken ct = default)
        => await hubContext.Clients.Group($"user:{userId}").SendAsync(eventName, payload, ct);

    public async Task BroadcastToRetroSessionAsync(Guid sessionId, string eventName, object payload, CancellationToken ct = default)
        => await hubContext.Clients.Group($"retro:{sessionId}").SendAsync(eventName, payload, ct);

    public async Task BroadcastToWorkItemAsync(Guid workItemId, string eventName, object payload, CancellationToken ct = default)
        => await hubContext.Clients.Group($"workitem:{workItemId}").SendAsync(eventName, payload, ct);
}
