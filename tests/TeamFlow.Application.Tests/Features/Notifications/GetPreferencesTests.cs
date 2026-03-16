using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Notifications.GetPreferences;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Tests.Features.Notifications;

public sealed class GetPreferencesTests
{
    private readonly INotificationPreferenceRepository _prefRepo = Substitute.For<INotificationPreferenceRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public GetPreferencesTests()
    {
        _currentUser.Id.Returns(ActorId);
    }

    private GetPreferencesHandler CreateHandler() => new(_prefRepo, _currentUser);

    [Fact]
    public async Task Handle_UserHasPreferences_ReturnsSavedValues()
    {
        var prefs = new List<NotificationPreference>
        {
            new() { UserId = ActorId, NotificationType = NotificationType.WorkItemAssigned, EmailEnabled = false, InAppEnabled = true },
            new() { UserId = ActorId, NotificationType = NotificationType.SprintSummary, EmailEnabled = true, InAppEnabled = false },
        };
        _prefRepo.GetByUserAsync(ActorId, Arg.Any<CancellationToken>()).Returns(prefs);

        var result = await CreateHandler().Handle(new GetPreferencesQuery(), CancellationToken.None);

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
        _prefRepo.GetByUserAsync(ActorId, Arg.Any<CancellationToken>())
            .Returns(new List<NotificationPreference>());

        var result = await CreateHandler().Handle(new GetPreferencesQuery(), CancellationToken.None);

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
        var prefs = new List<NotificationPreference>
        {
            new() { UserId = ActorId, NotificationType = NotificationType.MentionNotification, EmailEnabled = false, InAppEnabled = false },
        };
        _prefRepo.GetByUserAsync(ActorId, Arg.Any<CancellationToken>()).Returns(prefs);

        var result = await CreateHandler().Handle(new GetPreferencesQuery(), CancellationToken.None);

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
