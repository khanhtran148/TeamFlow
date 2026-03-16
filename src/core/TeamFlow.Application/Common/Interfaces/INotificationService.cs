using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Common.Interfaces;

public interface INotificationService
{
    Task CreateNotificationAsync(
        Guid recipientId,
        NotificationType type,
        string title,
        string? body,
        Guid? referenceId,
        string? referenceType,
        Guid? projectId,
        CancellationToken ct = default);
}
