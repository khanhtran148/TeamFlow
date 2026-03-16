namespace TeamFlow.Application.Features.Users;

public sealed record ActivityLogItemDto(
    Guid Id,
    Guid WorkItemId,
    string WorkItemTitle,
    string ActionType,
    string? FieldName,
    string? OldValue,
    string? NewValue,
    DateTime CreatedAt
);
