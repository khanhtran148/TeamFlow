using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Notifications.MarkAsRead;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Notifications;

public sealed class MarkAsReadTests
{
    private readonly IInAppNotificationRepository _notifRepo = Substitute.For<IInAppNotificationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public MarkAsReadTests()
    {
        _currentUser.Id.Returns(ActorId);
    }

    private MarkAsReadHandler CreateHandler() => new(_notifRepo, _currentUser);

    [Fact]
    public async Task Handle_OwnNotification_MarksAsRead()
    {
        var notification = InAppNotificationBuilder.New().WithRecipient(ActorId).Build();
        _notifRepo.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        var result = await CreateHandler().Handle(new MarkAsReadCommand(notification.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _notifRepo.Received(1).MarkAsReadAsync(notification.Id, ActorId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OtherUsersNotification_ReturnsAccessDenied()
    {
        var otherUserId = Guid.NewGuid();
        var notification = InAppNotificationBuilder.New().WithRecipient(otherUserId).Build();
        _notifRepo.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        var result = await CreateHandler().Handle(new MarkAsReadCommand(notification.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
        await _notifRepo.DidNotReceive().MarkAsReadAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotificationNotFound_ReturnsNotFound()
    {
        _notifRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((InAppNotification?)null);

        var result = await CreateHandler().Handle(new MarkAsReadCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
