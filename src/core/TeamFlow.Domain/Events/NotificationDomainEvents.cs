using MediatR;

namespace TeamFlow.Domain.Events;

public record NotificationCreatedDomainEvent(
    Guid NotificationId,
    Guid RecipientId,
    string Type,
    string Title
) : INotification;
