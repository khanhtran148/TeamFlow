namespace TeamFlow.Application.Features.PlanningPoker;

public sealed record PokerSessionDto(
    Guid Id,
    Guid WorkItemId,
    Guid ProjectId,
    Guid FacilitatorId,
    string FacilitatorName,
    bool IsRevealed,
    decimal? FinalEstimate,
    Guid? ConfirmedById,
    int VoteCount,
    IReadOnlyList<PokerVoteDto> Votes,
    DateTime CreatedAt,
    DateTime? ClosedAt
);

public sealed record PokerVoteDto(
    Guid Id,
    Guid VoterId,
    string VoterName,
    decimal? Value
);
