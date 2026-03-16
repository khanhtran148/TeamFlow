using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Common;

public sealed class TestCurrentUser : ICurrentUser
{
    public Guid Id => new("00000000-0000-0000-0000-000000000001");
    public string Email => "test@teamflow.dev";
    public string Name => "Test User";
    public bool IsAuthenticated => true;
}

public sealed class AlwaysAllowTestPermissionChecker : IPermissionChecker
{
    public Task<bool> HasPermissionAsync(Guid userId, Guid projectId, Permission permission, CancellationToken ct = default)
        => Task.FromResult(true);

    public Task<ProjectRole?> GetEffectiveRoleAsync(Guid userId, Guid projectId, CancellationToken ct = default)
        => Task.FromResult<ProjectRole?>(ProjectRole.Developer);
}

public sealed class NullBroadcastService : IBroadcastService
{
    public Task BroadcastToProjectAsync(Guid projectId, string eventName, object payload, CancellationToken ct = default) => Task.CompletedTask;
    public Task BroadcastToSprintAsync(Guid sprintId, string eventName, object payload, CancellationToken ct = default) => Task.CompletedTask;
    public Task BroadcastToUserAsync(Guid userId, string eventName, object payload, CancellationToken ct = default) => Task.CompletedTask;
    public Task BroadcastToRetroSessionAsync(Guid sessionId, string eventName, object payload, CancellationToken ct = default) => Task.CompletedTask;
    public Task BroadcastToWorkItemAsync(Guid workItemId, string eventName, object payload, CancellationToken ct = default) => Task.CompletedTask;
}

public sealed class TestHistoryService : IHistoryService
{
    public Task RecordAsync(WorkItemHistoryEntry entry, CancellationToken ct = default) => Task.CompletedTask;
}
