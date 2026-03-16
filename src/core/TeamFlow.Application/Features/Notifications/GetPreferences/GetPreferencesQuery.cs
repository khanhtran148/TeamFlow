using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Notifications.GetPreferences;

public sealed record GetPreferencesQuery : IRequest<Result<IReadOnlyList<NotificationPreferenceDto>>>;
