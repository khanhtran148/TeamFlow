using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface IInAppNotificationRepository
{
    Task<InAppNotification> AddAsync(InAppNotification notification, CancellationToken ct = default);
    Task<(IEnumerable<InAppNotification> Items, int TotalCount)> GetByRecipientPagedAsync(
        Guid recipientId,
        bool? isRead,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task MarkAsReadAsync(Guid id, Guid recipientId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid recipientId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid recipientId, CancellationToken ct = default);
}
