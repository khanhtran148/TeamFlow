using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Notifications.GetNotifications;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Notifications;

public sealed class GetNotificationsTests
{
    private readonly IInAppNotificationRepository _notifRepo = Substitute.For<IInAppNotificationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public GetNotificationsTests()
    {
        _currentUser.Id.Returns(ActorId);
    }

    private GetNotificationsHandler CreateHandler() => new(_notifRepo, _currentUser);

    [Fact]
    public async Task Handle_ReturnsCurrentUserNotifications()
    {
        var notifications = new[]
        {
            InAppNotificationBuilder.New().WithRecipient(ActorId).Build(),
            InAppNotificationBuilder.New().WithRecipient(ActorId).Build()
        };
        _notifRepo.GetByRecipientPagedAsync(ActorId, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns((notifications.AsEnumerable(), 2));

        var result = await CreateHandler().Handle(new GetNotificationsQuery(null, 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_FilterByIsRead_PassesFilter(bool isRead)
    {
        _notifRepo.GetByRecipientPagedAsync(ActorId, isRead, 1, 20, Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<InAppNotification>(), 0));

        var result = await CreateHandler().Handle(new GetNotificationsQuery(isRead, 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _notifRepo.Received(1).GetByRecipientPagedAsync(ActorId, isRead, 1, 20, Arg.Any<CancellationToken>());
    }
}
