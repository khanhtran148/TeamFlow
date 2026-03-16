using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Notifications;
using TeamFlow.Application.Features.Notifications.UpdatePreferences;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Notifications;

public sealed class UpdatePreferencesTests
{
    private readonly INotificationPreferenceRepository _repo = Substitute.For<INotificationPreferenceRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public UpdatePreferencesTests()
    {
        _currentUser.Id.Returns(ActorId);
    }

    private UpdatePreferencesHandler CreateHandler() => new(_repo, _currentUser);

    [Fact]
    public async Task Handle_NewPreference_CreatesIt()
    {
        _repo.GetByUserAndTypeAsync(ActorId, "WorkItemAssigned", Arg.Any<CancellationToken>())
            .Returns((NotificationPreference?)null);

        var prefs = new List<NotificationPreferenceDto>
        {
            new("WorkItemAssigned", false, true)
        };

        var result = await CreateHandler().Handle(new UpdatePreferencesCommand(prefs), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).UpsertAsync(
            Arg.Is<NotificationPreference>(p => p.NotificationType == "WorkItemAssigned" && !p.EmailEnabled),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingPreference_UpdatesIt()
    {
        var existing = new NotificationPreference
        {
            UserId = ActorId,
            NotificationType = "WorkItemAssigned",
            EmailEnabled = true,
            InAppEnabled = true
        };
        _repo.GetByUserAndTypeAsync(ActorId, "WorkItemAssigned", Arg.Any<CancellationToken>())
            .Returns(existing);

        var prefs = new List<NotificationPreferenceDto>
        {
            new("WorkItemAssigned", false, false)
        };

        var result = await CreateHandler().Handle(new UpdatePreferencesCommand(prefs), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).UpsertAsync(
            Arg.Is<NotificationPreference>(p => !p.EmailEnabled && !p.InAppEnabled),
            Arg.Any<CancellationToken>());
    }
}
