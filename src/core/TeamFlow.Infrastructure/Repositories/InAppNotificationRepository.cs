using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class InAppNotificationRepository(TeamFlowDbContext context) : IInAppNotificationRepository
{
    public async Task<InAppNotification> AddAsync(InAppNotification notification, CancellationToken ct = default)
    {
        context.InAppNotifications.Add(notification);
        await context.SaveChangesAsync(ct);
        return notification;
    }

    public async Task<(IEnumerable<InAppNotification> Items, int TotalCount)> GetByRecipientPagedAsync(
        Guid recipientId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.InAppNotifications
            .AsNoTracking()
            .Where(n => n.RecipientId == recipientId);

        if (isRead.HasValue)
            query = query.Where(n => n.IsRead == isRead.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task MarkAsReadAsync(Guid id, Guid recipientId, CancellationToken ct = default)
    {
        await context.InAppNotifications
            .Where(n => n.Id == id && n.RecipientId == recipientId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }

    public async Task MarkAllAsReadAsync(Guid recipientId, CancellationToken ct = default)
    {
        await context.InAppNotifications
            .Where(n => n.RecipientId == recipientId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }

    public async Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken ct = default)
        => await context.InAppNotifications
            .CountAsync(n => n.RecipientId == recipientId && !n.IsRead, ct);
}
