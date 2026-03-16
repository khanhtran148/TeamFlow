using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Common.Interfaces;

public interface INotificationPreferenceRepository
{
    Task<IReadOnlyList<NotificationPreference>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<NotificationPreference?> GetByUserAndTypeAsync(Guid userId, NotificationType notificationType, CancellationToken ct = default);
    Task UpsertAsync(NotificationPreference preference, CancellationToken ct = default);
}
