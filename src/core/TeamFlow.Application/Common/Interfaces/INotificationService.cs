namespace TeamFlow.Application.Common.Interfaces;

public interface INotificationService
{
    Task CreateNotificationAsync(
        Guid recipientId,
        string type,
        string title,
        string? body,
        Guid? referenceId,
        string? referenceType,
        Guid? projectId,
        CancellationToken ct = default);
}
