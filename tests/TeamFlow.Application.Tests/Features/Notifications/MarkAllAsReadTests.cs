using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Notifications.MarkAllAsRead;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Notifications;

[Collection("Social")]
public sealed class MarkAllAsReadTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_MarksAllForCurrentUser()
    {
        DbContext.Set<TeamFlow.Domain.Entities.InAppNotification>().AddRange(
            InAppNotificationBuilder.New().WithRecipient(SeedUserId).Build(),
            InAppNotificationBuilder.New().WithRecipient(SeedUserId).Build()
        );
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new MarkAllAsReadCommand());

        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var notifications = await DbContext.Set<TeamFlow.Domain.Entities.InAppNotification>()
            .Where(n => n.RecipientId == SeedUserId)
            .ToListAsync();
        notifications.Should().AllSatisfy(n => n.IsRead.Should().BeTrue());
    }

    [Fact]
    public async Task Handle_DoesNotMarkOtherUsersNotifications()
    {
        var otherUser = UserBuilder.New().WithEmail("markallread-other@example.com").Build();
        DbContext.Users.Add(otherUser);
        await DbContext.SaveChangesAsync();

        var otherUserId = otherUser.Id;
        DbContext.Set<TeamFlow.Domain.Entities.InAppNotification>().AddRange(
            InAppNotificationBuilder.New().WithRecipient(SeedUserId).Build(),
            InAppNotificationBuilder.New().WithRecipient(otherUserId).Build()
        );
        await DbContext.SaveChangesAsync();

        await Sender.Send(new MarkAllAsReadCommand());

        var otherNotifications = await DbContext.Set<TeamFlow.Domain.Entities.InAppNotification>()
            .Where(n => n.RecipientId == otherUserId)
            .ToListAsync();
        otherNotifications.Should().AllSatisfy(n => n.IsRead.Should().BeFalse());
    }
}
