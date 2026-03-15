using MediatR;

namespace TeamFlow.Domain.Events;

public record SprintStartedDomainEvent(
    Guid SprintId,
    Guid ProjectId,
    string SprintName,
    string? Goal,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid ActorId
) : INotification;

public record SprintCompletedDomainEvent(
    Guid SprintId,
    Guid ProjectId,
    string SprintName,
    int PlannedPoints,
    int CompletedPoints,
    Guid ActorId
) : INotification;

public record SprintItemAddedDomainEvent(
    Guid SprintId,
    Guid WorkItemId,
    Guid ProjectId,
    Guid ActorId
) : INotification;

public record SprintItemRemovedDomainEvent(
    Guid SprintId,
    Guid WorkItemId,
    Guid ProjectId,
    Guid ActorId
) : INotification;
