using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface INotificationPreferenceRepository
{
    Task<IReadOnlyList<NotificationPreference>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<NotificationPreference?> GetByUserAndTypeAsync(Guid userId, string notificationType, CancellationToken ct = default);
    Task UpsertAsync(NotificationPreference preference, CancellationToken ct = default);
}
