using MediatR;

namespace TeamFlow.Domain.Events;

public record RetroSessionStartedDomainEvent(
    Guid SessionId,
    Guid ProjectId,
    Guid? SprintId,
    Guid FacilitatorId
) : INotification;

public record RetroCardSubmittedDomainEvent(
    Guid SessionId,
    Guid CardId,
    Guid AuthorId
) : INotification;

public record RetroCardsRevealedDomainEvent(
    Guid SessionId,
    Guid ProjectId,
    int CardCount,
    Guid FacilitatorId
) : INotification;

public record RetroVoteCastDomainEvent(
    Guid SessionId,
    Guid CardId,
    Guid VoterId,
    short VoteCount
) : INotification;

public record RetroActionItemCreatedDomainEvent(
    Guid SessionId,
    Guid ActionItemId,
    string Title,
    Guid? AssigneeId,
    Guid ActorId
) : INotification;

public record RetroSessionClosedDomainEvent(
    Guid SessionId,
    Guid ProjectId,
    Guid? SprintId,
    int CardCount,
    int ActionItemCount,
    Guid FacilitatorId
) : INotification;
