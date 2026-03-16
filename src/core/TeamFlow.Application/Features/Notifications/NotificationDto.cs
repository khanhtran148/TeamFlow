namespace TeamFlow.Application.Features.Notifications;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string? Body,
    Guid? ReferenceId,
    string? ReferenceType,
    bool IsRead,
    DateTime CreatedAt
);

public sealed record NotificationPreferenceDto(
    string NotificationType,
    bool EmailEnabled,
    bool InAppEnabled
);

public sealed record UnreadCountDto(int Count);
