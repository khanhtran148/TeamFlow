using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Features.Notifications;
using TeamFlow.Application.Features.Notifications.UpdatePreferences;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;

namespace TeamFlow.Application.Tests.Features.Notifications;

[Collection("Social")]
public sealed class UpdatePreferencesTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_NewPreference_CreatesIt()
    {
        var prefs = new List<NotificationPreferenceDto>
        {
            new("WorkItemAssigned", false, true)
        };

        var result = await Sender.Send(new UpdatePreferencesCommand(prefs));

        result.IsSuccess.Should().BeTrue();
        var saved = await DbContext.Set<TeamFlow.Domain.Entities.NotificationPreference>()
            .SingleOrDefaultAsync(p => p.UserId == SeedUserId && p.NotificationType == NotificationType.WorkItemAssigned);
        saved.Should().NotBeNull();
        saved!.EmailEnabled.Should().BeFalse();
        saved.InAppEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ExistingPreference_UpdatesIt()
    {
        DbContext.Set<TeamFlow.Domain.Entities.NotificationPreference>().Add(
            new TeamFlow.Domain.Entities.NotificationPreference
            {
                UserId = SeedUserId,
                NotificationType = NotificationType.WorkItemAssigned,
                EmailEnabled = true,
                InAppEnabled = true
            }
        );
        await DbContext.SaveChangesAsync();

        var prefs = new List<NotificationPreferenceDto>
        {
            new("WorkItemAssigned", false, false)
        };

        var result = await Sender.Send(new UpdatePreferencesCommand(prefs));

        result.IsSuccess.Should().BeTrue();
        var updated = await DbContext.Set<TeamFlow.Domain.Entities.NotificationPreference>()
            .SingleAsync(p => p.UserId == SeedUserId && p.NotificationType == NotificationType.WorkItemAssigned);
        updated.EmailEnabled.Should().BeFalse();
        updated.InAppEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_InvalidNotificationType_ReturnsFailure()
    {
        var prefs = new List<NotificationPreferenceDto>
        {
            new("InvalidType", false, true)
        };

        var result = await Sender.Send(new UpdatePreferencesCommand(prefs));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid notification type");
    }
}
