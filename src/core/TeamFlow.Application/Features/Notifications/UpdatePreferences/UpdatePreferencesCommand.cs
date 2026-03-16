using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Notifications.UpdatePreferences;

public sealed record UpdatePreferencesCommand(
    IReadOnlyList<NotificationPreferenceDto> Preferences
) : IRequest<Result>;
