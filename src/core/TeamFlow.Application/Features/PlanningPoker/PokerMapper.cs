using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.PlanningPoker;

internal static class PokerMapper
{
    public static PokerSessionDto ToDto(PlanningPokerSession session)
    {
        var isRevealed = session.IsRevealed;
        var votes = session.Votes?.Select(v => ToVoteDto(v, isRevealed)).ToList() ?? [];

        return new PokerSessionDto(
            session.Id,
            session.WorkItemId,
            session.ProjectId,
            session.FacilitatorId,
            session.Facilitator?.Name ?? "Unknown",
            session.IsRevealed,
            session.FinalEstimate,
            session.ConfirmedById,
            votes.Count,
            votes,
            session.CreatedAt,
            session.ClosedAt
        );
    }

    public static PokerVoteDto ToVoteDto(PlanningPokerVote vote, bool isRevealed) =>
        new(
            vote.Id,
            vote.VoterId,
            vote.Voter?.Name ?? "Unknown",
            isRevealed ? vote.Value : null
        );
}
