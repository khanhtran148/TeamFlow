using MediatR;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Events;

public record ReleaseCreatedDomainEvent(
    Guid ReleaseId,
    Guid ProjectId,
    string ReleaseName,
    Guid ActorId
) : INotification;

public record ReleaseItemAssignedDomainEvent(
    Guid ReleaseId,
    Guid WorkItemId,
    Guid ProjectId,
    Guid ActorId
) : INotification;

public record ReleaseStatusChangedDomainEvent(
    Guid ReleaseId,
    Guid ProjectId,
    ReleaseStatus FromStatus,
    ReleaseStatus ToStatus,
    Guid ActorId
) : INotification;

public record ReleaseOverdueDetectedDomainEvent(
    Guid ReleaseId,
    Guid ProjectId,
    string ReleaseName,
    DateOnly ReleaseDate,
    int IncompleteItemCount
) : INotification;
