using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class PlanningPokerSessionRepository(TeamFlowDbContext context) : IPlanningPokerSessionRepository
{
    public async Task<PlanningPokerSession?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.PlanningPokerSessions
            .Include(s => s.Facilitator)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<PlanningPokerSession?> GetByIdWithVotesAsync(Guid id, CancellationToken ct = default)
        => await context.PlanningPokerSessions
            .Include(s => s.Facilitator)
            .Include(s => s.Votes)
                .ThenInclude(v => v.Voter)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<PlanningPokerSession?> GetActiveByWorkItemAsync(Guid workItemId, CancellationToken ct = default)
        => await context.PlanningPokerSessions
            .Include(s => s.Facilitator)
            .Include(s => s.Votes)
                .ThenInclude(v => v.Voter)
            .FirstOrDefaultAsync(s => s.WorkItemId == workItemId && s.ClosedAt == null, ct);

    public async Task<PlanningPokerSession> AddAsync(PlanningPokerSession session, CancellationToken ct = default)
    {
        context.PlanningPokerSessions.Add(session);
        await context.SaveChangesAsync(ct);
        return session;
    }

    public async Task<PlanningPokerSession> UpdateAsync(PlanningPokerSession session, CancellationToken ct = default)
    {
        context.PlanningPokerSessions.Update(session);
        await context.SaveChangesAsync(ct);
        return session;
    }

    public async Task<PlanningPokerVote?> GetVoteAsync(Guid sessionId, Guid voterId, CancellationToken ct = default)
        => await context.PlanningPokerVotes
            .FirstOrDefaultAsync(v => v.SessionId == sessionId && v.VoterId == voterId, ct);

    public async Task<PlanningPokerVote> AddVoteAsync(PlanningPokerVote vote, CancellationToken ct = default)
    {
        context.PlanningPokerVotes.Add(vote);
        await context.SaveChangesAsync(ct);
        return vote;
    }

    public async Task<PlanningPokerVote> UpdateVoteAsync(PlanningPokerVote vote, CancellationToken ct = default)
    {
        context.PlanningPokerVotes.Update(vote);
        await context.SaveChangesAsync(ct);
        return vote;
    }

    public async Task<int> GetVoteCountAsync(Guid sessionId, CancellationToken ct = default)
        => await context.PlanningPokerVotes
            .CountAsync(v => v.SessionId == sessionId, ct);
}
