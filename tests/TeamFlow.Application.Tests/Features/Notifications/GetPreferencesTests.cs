using FluentAssertions;
using TeamFlow.Application.Features.Notifications.GetPreferences;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;

namespace TeamFlow.Application.Tests.Features.Notifications;

[Collection("Social")]
public sealed class GetPreferencesTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_UserHasPreferences_ReturnsSavedValues()
    {
        DbContext.Set<TeamFlow.Domain.Entities.NotificationPreference>().AddRange(
            new TeamFlow.Domain.Entities.NotificationPreference
            {
                UserId = SeedUserId,
                NotificationType = NotificationType.WorkItemAssigned,
                EmailEnabled = false,
                InAppEnabled = true
            },
            new TeamFlow.Domain.Entities.NotificationPreference
            {
                UserId = SeedUserId,
                NotificationType = NotificationType.SprintSummary,
                EmailEnabled = true,
                InAppEnabled = false
            }
        );
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetPreferencesQuery());

        result.IsSuccess.Should().BeTrue();
        var allTypes = Enum.GetValues<NotificationType>();
        result.Value.Should().HaveCount(allTypes.Length);

        var assignedPref = result.Value.Single(p => p.NotificationType == nameof(NotificationType.WorkItemAssigned));
        assignedPref.EmailEnabled.Should().BeFalse();
        assignedPref.InAppEnabled.Should().BeTrue();

        var sprintPref = result.Value.Single(p => p.NotificationType == nameof(NotificationType.SprintSummary));
        sprintPref.EmailEnabled.Should().BeTrue();
        sprintPref.InAppEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NoPreferencesStored_ReturnsDefaultsAllEnabled()
    {
        var result = await Sender.Send(new GetPreferencesQuery());

        result.IsSuccess.Should().BeTrue();
        var allTypes = Enum.GetValues<NotificationType>();
        result.Value.Should().HaveCount(allTypes.Length);
        result.Value.Should().AllSatisfy(p =>
        {
            p.EmailEnabled.Should().BeTrue();
            p.InAppEnabled.Should().BeTrue();
        });
    }

    [Fact]
    public async Task Handle_PartialPreferences_DefaultsMissingTypes()
    {
        DbContext.Set<TeamFlow.Domain.Entities.NotificationPreference>().Add(
            new TeamFlow.Domain.Entities.NotificationPreference
            {
                UserId = SeedUserId,
                NotificationType = NotificationType.MentionNotification,
                EmailEnabled = false,
                InAppEnabled = false
            }
        );
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetPreferencesQuery());

        result.IsSuccess.Should().BeTrue();
        var mentionPref = result.Value.Single(p => p.NotificationType == nameof(NotificationType.MentionNotification));
        mentionPref.EmailEnabled.Should().BeFalse();
        mentionPref.InAppEnabled.Should().BeFalse();

        var otherPrefs = result.Value.Where(p => p.NotificationType != nameof(NotificationType.MentionNotification));
        otherPrefs.Should().AllSatisfy(p =>
        {
            p.EmailEnabled.Should().BeTrue();
            p.InAppEnabled.Should().BeTrue();
        });
    }
}
