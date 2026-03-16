using FluentAssertions;
using TeamFlow.Application.Features.Notifications.GetUnreadCount;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Notifications;

[Collection("Social")]
public sealed class GetUnreadCountTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_NoUnreadNotifications_ReturnsZero()
    {
        var result = await Sender.Send(new GetUnreadCountQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithUnreadNotifications_ReturnsCorrectCount()
    {
        DbContext.Set<TeamFlow.Domain.Entities.InAppNotification>().AddRange(
            InAppNotificationBuilder.New().WithRecipient(SeedUserId).Build(),
            InAppNotificationBuilder.New().WithRecipient(SeedUserId).Build(),
            InAppNotificationBuilder.New().WithRecipient(SeedUserId).Build(),
            InAppNotificationBuilder.New().WithRecipient(SeedUserId).Read().Build()
        );
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetUnreadCountQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(3);
    }

    [Fact]
    public async Task Handle_QueriesCurrentUserOnly()
    {
        var otherUser = UserBuilder.New().WithEmail("getunread-other@example.com").Build();
        DbContext.Users.Add(otherUser);
        await DbContext.SaveChangesAsync();

        var otherUserId = otherUser.Id;
        DbContext.Set<TeamFlow.Domain.Entities.InAppNotification>().AddRange(
            InAppNotificationBuilder.New().WithRecipient(SeedUserId).Build(),
            InAppNotificationBuilder.New().WithRecipient(otherUserId).Build(),
            InAppNotificationBuilder.New().WithRecipient(otherUserId).Build()
        );
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetUnreadCountQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Count.Should().Be(1);
    }
}
