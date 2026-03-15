using MediatR;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Events;

public record WorkItemCreatedDomainEvent(
    Guid WorkItemId,
    Guid ProjectId,
    WorkItemType Type,
    string Title,
    Guid ActorId
) : INotification;

public record WorkItemStatusChangedDomainEvent(
    Guid WorkItemId,
    Guid ProjectId,
    WorkItemStatus FromStatus,
    WorkItemStatus ToStatus,
    Guid? SprintId,
    Guid ActorId
) : INotification;

public record WorkItemEstimationChangedDomainEvent(
    Guid WorkItemId,
    Guid ProjectId,
    decimal? OldValue,
    decimal? NewValue,
    string? Source,
    Guid ActorId
) : INotification;

public record WorkItemAssignedDomainEvent(
    Guid WorkItemId,
    Guid ProjectId,
    Guid? OldAssigneeId,
    Guid NewAssigneeId,
    Guid ActorId
) : INotification;

public record WorkItemUnassignedDomainEvent(
    Guid WorkItemId,
    Guid ProjectId,
    Guid PreviousAssigneeId,
    Guid ActorId
) : INotification;

public record WorkItemPriorityChangedDomainEvent(
    Guid WorkItemId,
    Guid ProjectId,
    Priority? OldPriority,
    Priority? NewPriority,
    Guid ActorId
) : INotification;

public record WorkItemLinkAddedDomainEvent(
    Guid WorkItemId,
    Guid LinkedItemId,
    LinkType LinkType,
    Guid ActorId
) : INotification;

public record WorkItemLinkRemovedDomainEvent(
    Guid WorkItemId,
    Guid LinkedItemId,
    LinkType LinkType,
    Guid ActorId
) : INotification;

public record WorkItemRejectedDomainEvent(
    Guid WorkItemId,
    Guid ProjectId,
    string? RejectionReason,
    Guid ActorId
) : INotification;

public record WorkItemNeedsClarificationFlaggedDomainEvent(
    Guid WorkItemId,
    Guid ProjectId,
    string? Notes,
    Guid ActorId
) : INotification;

public record WorkItemStaleFlaggedDomainEvent(
    Guid WorkItemId,
    Guid ProjectId,
    string Severity,
    int DaysSinceUpdate
) : INotification;
