using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using TeamFlow.Application.Common.Behaviors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Common;

internal sealed record TestBehaviorRequest : IRequest<Result>;
internal sealed record TestBehaviorRequestWithValue : IRequest<Result<string>>;

/// <summary>
/// Minimal stub for IUserRepository — returns a pre-configured user by ID.
/// Avoids NSubstitute for this pure unit test of the pipeline behavior.
/// </summary>
internal sealed class StubUserRepository(User? user) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(user);

    public Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<User>>([]);

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult<User?>(null);

    public Task<IEnumerable<User>> GetByDisplayNamesAsync(IEnumerable<string> displayNames, CancellationToken ct = default) =>
        Task.FromResult(Enumerable.Empty<User>());

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(false);

    public Task<User> AddAsync(User user, CancellationToken ct = default) =>
        Task.FromResult(user);

    public Task UpdateAsync(User user, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<IEnumerable<User>> ListAllAsync(CancellationToken ct = default) =>
        Task.FromResult(Enumerable.Empty<User>());

    public Task<(IEnumerable<User> Items, int TotalCount)> ListPagedAsync(
        string? search, int page, int pageSize, CancellationToken ct = default) =>
        Task.FromResult((Enumerable.Empty<User>(), 0));
}

/// <summary>Minimal stub for ICurrentUser.</summary>
internal sealed class StubCurrentUser(bool isAuthenticated, Guid id) : ICurrentUser
{
    public Guid Id => id;
    public string Email => "stub@example.com";
    public string Name => "Stub User";
    public bool IsAuthenticated => isAuthenticated;
    public TeamFlow.Domain.Enums.SystemRole SystemRole => TeamFlow.Domain.Enums.SystemRole.User;
}

public sealed class ActiveUserBehaviorTests
{
    [Fact]
    public async Task Handle_AuthenticatedActiveUser_PassesThrough()
    {
        var userId = Guid.NewGuid();
        var activeUser = UserBuilder.New().WithIsActive(true).Build();
        var currentUser = new StubCurrentUser(true, userId);
        var userRepo = new StubUserRepository(activeUser);

        var behavior = new ActiveUserBehavior<TestBehaviorRequest, Result>(currentUser, userRepo);
        var called = false;
        var result = await behavior.Handle(
            new TestBehaviorRequest(),
            ct => { called = true; return Task.FromResult(Result.Success()); },
            CancellationToken.None);

        called.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UnauthenticatedRequest_PassesThrough()
    {
        var currentUser = new StubCurrentUser(false, Guid.NewGuid());
        var userRepo = new StubUserRepository(null);

        var behavior = new ActiveUserBehavior<TestBehaviorRequest, Result>(currentUser, userRepo);
        var called = false;
        var result = await behavior.Handle(
            new TestBehaviorRequest(),
            ct => { called = true; return Task.FromResult(Result.Success()); },
            CancellationToken.None);

        called.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ReturnsForbidden()
    {
        var userId = Guid.NewGuid();
        var inactiveUser = UserBuilder.New().WithIsActive(false).Build();
        var currentUser = new StubCurrentUser(true, userId);
        var userRepo = new StubUserRepository(inactiveUser);

        var behavior = new ActiveUserBehavior<TestBehaviorRequest, Result>(currentUser, userRepo);
        var called = false;
        var result = await behavior.Handle(
            new TestBehaviorRequest(),
            ct => { called = true; return Task.FromResult(Result.Success()); },
            CancellationToken.None);

        called.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("deactivated");
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ReturnsGenericForbiddenForResultT()
    {
        var userId = Guid.NewGuid();
        var inactiveUser = UserBuilder.New().WithIsActive(false).Build();
        var currentUser = new StubCurrentUser(true, userId);
        var userRepo = new StubUserRepository(inactiveUser);

        var behavior = new ActiveUserBehavior<TestBehaviorRequestWithValue, Result<string>>(currentUser, userRepo);
        var called = false;
        var result = await behavior.Handle(
            new TestBehaviorRequestWithValue(),
            ct => { called = true; return Task.FromResult(Result.Success("value")); },
            CancellationToken.None);

        called.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.ToLowerInvariant().Should().Contain("deactivated");
    }
}
