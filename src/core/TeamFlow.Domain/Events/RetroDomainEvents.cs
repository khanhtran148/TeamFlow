using MediatR;

namespace TeamFlow.Domain.Events;

public record RetroSessionStartedDomainEvent(
    Guid SessionId,
    Guid ProjectId,
    Guid? SprintId,
    Guid FacilitatorId
) : INotification;

/// <summary>
/// Published when a retro card is submitted.
/// WARNING: AuthorId is included for internal processing only.
/// Any consumer that broadcasts this event to clients (e.g., SignalR, WebSocket)
/// MUST check the session's AnonymityMode and strip AuthorId when the session
/// is anonymous. Use <see cref="TeamFlow.Domain.Enums.RetroAnonymityModes"/> constants.
/// Failure to do so leaks the author's identity in anonymous retro sessions.
/// </summary>
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
