using FluentAssertions;
using TeamFlow.Application.Features.Notifications.GetNotifications;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Notifications;

[Collection("Social")]
public sealed class GetNotificationsTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ReturnsCurrentUserNotifications()
    {
        DbContext.Set<TeamFlow.Domain.Entities.InAppNotification>().AddRange(
            InAppNotificationBuilder.New().WithRecipient(SeedUserId).Build(),
            InAppNotificationBuilder.New().WithRecipient(SeedUserId).Build()
        );
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetNotificationsQuery(null, 1, 20));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_FilterByIsRead_ReturnsFilteredNotifications(bool isRead)
    {
        DbContext.Set<TeamFlow.Domain.Entities.InAppNotification>().AddRange(
            InAppNotificationBuilder.New().WithRecipient(SeedUserId).Build(),
            InAppNotificationBuilder.New().WithRecipient(SeedUserId).Read().Build()
        );
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetNotificationsQuery(isRead, 1, 20));

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().AllSatisfy(n => n.IsRead.Should().Be(isRead));
    }
}
