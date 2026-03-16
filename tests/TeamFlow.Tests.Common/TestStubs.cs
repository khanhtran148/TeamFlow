using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Common;

public sealed class TestCurrentUser : ICurrentUser
{
    public Guid Id => new("00000000-0000-0000-0000-000000000001");
    public string Email => "test@teamflow.dev";
    public string Name => "Test User";
    public bool IsAuthenticated => true;
    public SystemRole SystemRole => SystemRole.User;
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

public sealed class AlwaysDenyTestPermissionChecker : IPermissionChecker
{
    public Task<bool> HasPermissionAsync(Guid userId, Guid projectId, Permission permission, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<ProjectRole?> GetEffectiveRoleAsync(Guid userId, Guid projectId, CancellationToken ct = default)
        => Task.FromResult<ProjectRole?>(null);
}

public sealed class TestAdminCurrentUser(Guid userId) : ICurrentUser
{
    public Guid Id => userId;
    public string Email => "admin@teamflow.dev";
    public string Name => "Admin User";
    public bool IsAuthenticated => true;
    public SystemRole SystemRole => SystemRole.SystemAdmin;
}

public sealed class CapturingPublisher : IPublisher
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<object> _published = new();
    public IReadOnlyList<object> Published => [.. _published];

    public Task Publish(object notification, CancellationToken ct = default)
    {
        _published.Enqueue(notification);
        return Task.CompletedTask;
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification
    {
        _published.Enqueue(notification!);
        return Task.CompletedTask;
    }

    public bool HasPublished<T>() => _published.OfType<T>().Any();
    public T GetPublished<T>() => _published.OfType<T>().Single();
    public IEnumerable<T> GetAllPublished<T>() => _published.OfType<T>();
}
