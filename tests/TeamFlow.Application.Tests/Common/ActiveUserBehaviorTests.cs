using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Behaviors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Common;

internal sealed record TestBehaviorRequest : IRequest<Result>;
internal sealed record TestBehaviorRequestWithValue : IRequest<Result<string>>;

public sealed class ActiveUserBehaviorTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();

    [Fact]
    public async Task Handle_AuthenticatedActiveUser_PassesThrough()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns(Guid.NewGuid());
        var activeUser = UserBuilder.New().WithIsActive(true).Build();
        _userRepo.GetByIdAsync(_currentUser.Id, Arg.Any<CancellationToken>()).Returns(activeUser);

        var behavior = new ActiveUserBehavior<TestBehaviorRequest, Result>(_currentUser, _userRepo);
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
        _currentUser.IsAuthenticated.Returns(false);

        var behavior = new ActiveUserBehavior<TestBehaviorRequest, Result>(_currentUser, _userRepo);
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
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns(Guid.NewGuid());
        var inactiveUser = UserBuilder.New().WithIsActive(false).Build();
        _userRepo.GetByIdAsync(_currentUser.Id, Arg.Any<CancellationToken>()).Returns(inactiveUser);

        var behavior = new ActiveUserBehavior<TestBehaviorRequest, Result>(_currentUser, _userRepo);
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
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns(Guid.NewGuid());
        var inactiveUser = UserBuilder.New().WithIsActive(false).Build();
        _userRepo.GetByIdAsync(_currentUser.Id, Arg.Any<CancellationToken>()).Returns(inactiveUser);

        var behavior = new ActiveUserBehavior<TestBehaviorRequestWithValue, Result<string>>(_currentUser, _userRepo);
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
