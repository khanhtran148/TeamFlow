using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Notifications.GetPreferences;

public sealed class GetPreferencesHandler(
    INotificationPreferenceRepository preferenceRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetPreferencesQuery, Result<IReadOnlyList<NotificationPreferenceDto>>>
{
    public async Task<Result<IReadOnlyList<NotificationPreferenceDto>>> Handle(
        GetPreferencesQuery request, CancellationToken ct)
    {
        var prefs = await preferenceRepository.GetByUserAsync(currentUser.Id, ct);

        var allTypes = Enum.GetValues<NotificationType>();
        var prefMap = prefs.ToDictionary(p => p.NotificationType, p => p);

        var dtos = allTypes.Select(t =>
        {
            var typeName = t.ToString();
            if (prefMap.TryGetValue(typeName, out var pref))
                return new NotificationPreferenceDto(typeName, pref.EmailEnabled, pref.InAppEnabled);
            return new NotificationPreferenceDto(typeName, true, true);
        }).ToList();

        return Result.Success<IReadOnlyList<NotificationPreferenceDto>>(dtos);
    }
}
