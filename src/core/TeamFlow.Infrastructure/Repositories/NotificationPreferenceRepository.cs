using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class NotificationPreferenceRepository(TeamFlowDbContext context) : INotificationPreferenceRepository
{
    public async Task<IReadOnlyList<NotificationPreference>> GetByUserAsync(
        Guid userId, CancellationToken ct = default)
        => await context.NotificationPreferences
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);

    public async Task<NotificationPreference?> GetByUserAndTypeAsync(
        Guid userId, NotificationType notificationType, CancellationToken ct = default)
        => await context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.NotificationType == notificationType, ct);

    public async Task UpsertAsync(NotificationPreference preference, CancellationToken ct = default)
    {
        var existing = await context.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == preference.UserId
                && p.NotificationType == preference.NotificationType, ct);

        if (existing is null)
        {
            context.NotificationPreferences.Add(preference);
        }
        else
        {
            existing.EmailEnabled = preference.EmailEnabled;
            existing.InAppEnabled = preference.InAppEnabled;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(ct);
    }
}
