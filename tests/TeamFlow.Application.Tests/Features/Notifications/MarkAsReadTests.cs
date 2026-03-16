using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Notifications.MarkAsRead;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Notifications;

[Collection("Social")]
public sealed class MarkAsReadTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_OwnNotification_MarksAsRead()
    {
        var notification = InAppNotificationBuilder.New().WithRecipient(SeedUserId).Build();
        DbContext.Set<TeamFlow.Domain.Entities.InAppNotification>().Add(notification);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new MarkAsReadCommand(notification.Id));

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var persisted = await DbContext.Set<TeamFlow.Domain.Entities.InAppNotification>()
            .SingleAsync(n => n.Id == notification.Id);
        persisted.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OtherUsersNotification_ReturnsAccessDenied()
    {
        var otherUser = UserBuilder.New().WithEmail("markread-other@example.com").Build();
        DbContext.Users.Add(otherUser);
        await DbContext.SaveChangesAsync();

        var notification = InAppNotificationBuilder.New().WithRecipient(otherUser.Id).Build();
        DbContext.Set<TeamFlow.Domain.Entities.InAppNotification>().Add(notification);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new MarkAsReadCommand(notification.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_NotificationNotFound_ReturnsNotFound()
    {
        var result = await Sender.Send(new MarkAsReadCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
