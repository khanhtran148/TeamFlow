using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Notifications.UpdatePreferences;

public sealed class UpdatePreferencesHandler(
    INotificationPreferenceRepository preferenceRepository,
    ICurrentUser currentUser)
    : IRequestHandler<UpdatePreferencesCommand, Result>
{
    public async Task<Result> Handle(UpdatePreferencesCommand request, CancellationToken ct)
    {
        foreach (var dto in request.Preferences)
        {
            if (!Enum.TryParse<NotificationType>(dto.NotificationType, out var notificationType))
                return Result.Failure($"Invalid notification type: {dto.NotificationType}");

            var pref = await preferenceRepository.GetByUserAndTypeAsync(
                currentUser.Id, notificationType, ct);

            if (pref is null)
            {
                pref = new NotificationPreference
                {
                    UserId = currentUser.Id,
                    NotificationType = notificationType,
                    EmailEnabled = dto.EmailEnabled,
                    InAppEnabled = dto.InAppEnabled
                };
            }
            else
            {
                pref.EmailEnabled = dto.EmailEnabled;
                pref.InAppEnabled = dto.InAppEnabled;
                pref.UpdatedAt = DateTime.UtcNow;
            }

            await preferenceRepository.UpsertAsync(pref, ct);
        }

        return Result.Success();
    }
}
