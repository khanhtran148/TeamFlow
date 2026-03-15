using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Api.Hubs;

/// <summary>
/// Main SignalR hub for TeamFlow realtime events.
/// Clients join groups on connection based on their current view.
/// All join methods validate permission before adding to group.
/// </summary>
[Authorize]
public sealed class TeamFlowHub(
    ILogger<TeamFlowHub> logger,
    IPermissionChecker permissionChecker,
    ICurrentUser currentUser,
    IWorkItemRepository workItemRepository) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>Join the project group to receive all project events.</summary>
    public async Task JoinProject(string projectId)
    {
        var id = ParseGuid(projectId);
        await EnsurePermissionAsync(id, Permission.Project_View);

        await Groups.AddToGroupAsync(Context.ConnectionId, $"project:{projectId}");
        logger.LogDebug("Connection {ConnectionId} joined project:{ProjectId}", Context.ConnectionId, projectId);
    }

    public async Task LeaveProject(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project:{projectId}");
    }

    /// <summary>Join the sprint board group.</summary>
    public async Task JoinSprint(string sprintId)
    {
        var id = ParseGuid(sprintId);
        // Sprint permission requires project-scoped check.
        // Sprint→Project lookup not yet available; deny until sprint repository is added.
        // TODO: Inject ISprintRepository and resolve projectId from sprint, then check Sprint_View.
        throw new HubException("Sprint group join requires project context. Use JoinProject instead.");
    }

    public async Task LeaveSprint(string sprintId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sprint:{sprintId}");
    }

    /// <summary>Join the work item detail page group.</summary>
    public async Task JoinWorkItem(string workItemId)
    {
        var id = ParseGuid(workItemId);

        // Resolve parent project from work item
        var workItem = await workItemRepository.GetByIdAsync(id);
        if (workItem is null)
            throw new HubException("Work item not found.");

        await EnsurePermissionAsync(workItem.ProjectId, Permission.WorkItem_View);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"workitem:{workItemId}");
    }

    public async Task LeaveWorkItem(string workItemId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workitem:{workItemId}");
    }

    /// <summary>Join a retro session group.</summary>
    public async Task JoinRetroSession(string sessionId)
    {
        ParseGuid(sessionId);
        // Retro→Project lookup not yet available; deny until retro repository is added.
        // TODO: Inject IRetroSessionRepository and resolve projectId, then check Retro_View.
        throw new HubException("Retro group join requires project context. Use JoinProject instead.");
    }

    public async Task LeaveRetroSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"retro:{sessionId}");
    }

    /// <summary>Join the personal notifications group.</summary>
    public async Task JoinUserNotifications(string userId)
    {
        if (currentUser.Id.ToString() != userId)
        {
            throw new HubException("Cannot join another user's notification group.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
    }

    private static Guid ParseGuid(string value)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new HubException("Invalid ID format.");
        return guid;
    }

    private async Task EnsurePermissionAsync(Guid projectId, Permission permission)
    {
        var allowed = await permissionChecker.HasPermissionAsync(
            currentUser.Id, projectId, permission);

        if (!allowed)
            throw new HubException("You do not have permission to join this group.");
    }
}
