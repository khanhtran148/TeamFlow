using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Notifications.MarkAllAsRead;

namespace TeamFlow.Application.Tests.Features.Notifications;

public sealed class MarkAllAsReadTests
{
    private readonly IInAppNotificationRepository _notifRepo = Substitute.For<IInAppNotificationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public MarkAllAsReadTests()
    {
        _currentUser.Id.Returns(ActorId);
    }

    private MarkAllAsReadHandler CreateHandler() => new(_notifRepo, _currentUser);

    [Fact]
    public async Task Handle_MarksAllForCurrentUser()
    {
        var result = await CreateHandler().Handle(new MarkAllAsReadCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _notifRepo.Received(1).MarkAllAsReadAsync(ActorId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotMarkOtherUsersNotifications()
    {
        var otherUserId = Guid.NewGuid();

        await CreateHandler().Handle(new MarkAllAsReadCommand(), CancellationToken.None);

        await _notifRepo.DidNotReceive().MarkAllAsReadAsync(otherUserId, Arg.Any<CancellationToken>());
    }
}
