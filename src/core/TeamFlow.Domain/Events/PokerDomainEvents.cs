using MediatR;

namespace TeamFlow.Domain.Events;

public sealed record PokerSessionCreatedDomainEvent(
    Guid SessionId,
    Guid WorkItemId,
    Guid ProjectId,
    Guid FacilitatorId
) : INotification;

public sealed record PokerVoteCastDomainEvent(
    Guid SessionId,
    Guid ProjectId,
    int TotalVoteCount
) : INotification;

public sealed record PokerVotesRevealedDomainEvent(
    Guid SessionId,
    Guid ProjectId,
    Guid FacilitatorId
) : INotification;

public sealed record PokerEstimateConfirmedDomainEvent(
    Guid SessionId,
    Guid WorkItemId,
    Guid ProjectId,
    decimal FinalEstimate,
    Guid ConfirmedById
) : INotification;
