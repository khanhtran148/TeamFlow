using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface IPlanningPokerSessionRepository
{
    Task<PlanningPokerSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PlanningPokerSession?> GetByIdWithVotesAsync(Guid id, CancellationToken ct = default);
    Task<PlanningPokerSession?> GetActiveByWorkItemAsync(Guid workItemId, CancellationToken ct = default);
    Task<PlanningPokerSession> AddAsync(PlanningPokerSession session, CancellationToken ct = default);
    Task<PlanningPokerSession> UpdateAsync(PlanningPokerSession session, CancellationToken ct = default);
    Task<PlanningPokerVote?> GetVoteAsync(Guid sessionId, Guid voterId, CancellationToken ct = default);
    Task<PlanningPokerVote> AddVoteAsync(PlanningPokerVote vote, CancellationToken ct = default);
    Task<PlanningPokerVote> UpdateVoteAsync(PlanningPokerVote vote, CancellationToken ct = default);
    Task<int> GetVoteCountAsync(Guid sessionId, CancellationToken ct = default);
}
