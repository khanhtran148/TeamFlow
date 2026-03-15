using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TeamFlow.Api.Hubs;

/// <summary>
/// Main SignalR hub for TeamFlow realtime events.
/// Clients join groups on connection based on their current view.
/// </summary>
[Authorize]
public class TeamFlowHub : Hub
{
    private readonly ILogger<TeamFlowHub> _logger;

    public TeamFlowHub(ILogger<TeamFlowHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Join the project group to receive all project events.</summary>
    public async Task JoinProject(string projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project:{projectId}");
        _logger.LogDebug("Connection {ConnectionId} joined project:{ProjectId}", Context.ConnectionId, projectId);
    }

    public async Task LeaveProject(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project:{projectId}");
    }

    /// <summary>Join the sprint board group.</summary>
    public async Task JoinSprint(string sprintId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"sprint:{sprintId}");
    }

    public async Task LeaveSprint(string sprintId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sprint:{sprintId}");
    }

    /// <summary>Join the work item detail page group.</summary>
    public async Task JoinWorkItem(string workItemId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"workitem:{workItemId}");
    }

    public async Task LeaveWorkItem(string workItemId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workitem:{workItemId}");
    }

    /// <summary>Join a retro session group.</summary>
    public async Task JoinRetroSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"retro:{sessionId}");
    }

    public async Task LeaveRetroSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"retro:{sessionId}");
    }

    /// <summary>Join the personal notifications group.</summary>
    public async Task JoinUserNotifications(string userId)
    {
        // Verify the user is joining their own group
        var currentUserId = Context.User?.FindFirst("sub")?.Value;
        if (currentUserId != userId)
        {
            throw new HubException("Cannot join another user's notification group.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
    }
}
