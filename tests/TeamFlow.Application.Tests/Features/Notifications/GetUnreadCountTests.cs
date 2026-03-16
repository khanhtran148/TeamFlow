using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Notifications.GetUnreadCount;

namespace TeamFlow.Application.Tests.Features.Notifications;

public sealed class GetUnreadCountTests
{
    private readonly IInAppNotificationRepository _notifRepo = Substitute.For<IInAppNotificationRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public GetUnreadCountTests()
    {
        _currentUser.Id.Returns(ActorId);
    }

    private GetUnreadCountHandler CreateHandler() => new(_notifRepo, _currentUser);

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(42)]
    public async Task Handle_ReturnsCorrectCount(int expectedCount)
    {
        _notifRepo.GetUnreadCountAsync(ActorId, Arg.Any<CancellationToken>()).Returns(expectedCount);

        var result = await CreateHandler().Handle(new GetUnreadCountQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(expectedCount);
    }

    [Fact]
    public async Task Handle_QueriesCurrentUserOnly()
    {
        _notifRepo.GetUnreadCountAsync(ActorId, Arg.Any<CancellationToken>()).Returns(3);

        await CreateHandler().Handle(new GetUnreadCountQuery(), CancellationToken.None);

        await _notifRepo.Received(1).GetUnreadCountAsync(ActorId, Arg.Any<CancellationToken>());
    }
}
