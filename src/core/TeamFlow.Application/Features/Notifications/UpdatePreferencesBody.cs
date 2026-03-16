namespace TeamFlow.Application.Features.Notifications;

public sealed record UpdatePreferencesBody(IReadOnlyList<NotificationPreferenceDto> Preferences);
