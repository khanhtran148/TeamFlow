using FluentAssertions;
using TeamFlow.Application.Features.Notifications;
using TeamFlow.Application.Features.Notifications.UpdatePreferences;

namespace TeamFlow.Application.Tests.Features.Notifications;

public sealed class UpdatePreferencesValidatorTests
{
    private readonly UpdatePreferencesValidator _validator = new();

    [Fact]
    public async Task Validate_ValidInput_Passes()
    {
        var cmd = new UpdatePreferencesCommand(
            [new NotificationPreferenceDto("WorkItemAssigned", true, true)]);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_NullPreferences_Fails()
    {
        var cmd = new UpdatePreferencesCommand(null!);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Preferences");
    }

    [Fact]
    public async Task Validate_EmptyPreferences_Fails()
    {
        var cmd = new UpdatePreferencesCommand([]);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Preferences");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyNotificationType_Fails(string? notificationType)
    {
        var cmd = new UpdatePreferencesCommand(
            [new NotificationPreferenceDto(notificationType!, true, true)]);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("NotificationType"));
    }

    [Fact]
    public async Task Validate_MultipleValidPreferences_Passes()
    {
        var cmd = new UpdatePreferencesCommand(
        [
            new NotificationPreferenceDto("WorkItemAssigned", true, false),
            new NotificationPreferenceDto("SprintStarted", false, true)
        ]);
        var result = await _validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}
