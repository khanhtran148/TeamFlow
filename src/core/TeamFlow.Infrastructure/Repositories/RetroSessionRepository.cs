using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class RetroSessionRepository(TeamFlowDbContext context) : IRetroSessionRepository
{
    public async Task<RetroSession?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.RetroSessions
            .Include(s => s.Facilitator)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<RetroSession?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await context.RetroSessions
            .Include(s => s.Facilitator)
            .Include(s => s.Cards)
                .ThenInclude(c => c.Votes)
            .Include(s => s.Cards)
                .ThenInclude(c => c.Author)
            .Include(s => s.ActionItems)
                .ThenInclude(a => a.Assignee)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<RetroSession> AddAsync(RetroSession session, CancellationToken ct = default)
    {
        context.RetroSessions.Add(session);
        await context.SaveChangesAsync(ct);
        return session;
    }

    public async Task<RetroSession> UpdateAsync(RetroSession session, CancellationToken ct = default)
    {
        context.RetroSessions.Update(session);
        await context.SaveChangesAsync(ct);
        return session;
    }

    public async Task DeleteAsync(RetroSession session, CancellationToken ct = default)
    {
        context.RetroSessions.Remove(session);
        await context.SaveChangesAsync(ct);
    }

    public async Task<(IEnumerable<RetroSession> Items, int TotalCount)> ListByProjectAsync(
        Guid projectId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.RetroSessions
            .AsNoTracking()
            .Include(s => s.Facilitator)
            .Where(s => s.ProjectId == projectId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<RetroSession?> GetLastClosedByProjectAsync(Guid projectId, CancellationToken ct = default)
        => await context.RetroSessions
            .Include(s => s.ActionItems)
                .ThenInclude(a => a.Assignee)
            .Where(s => s.ProjectId == projectId && s.Status == RetroSessionStatus.Closed)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<RetroCard?> GetCardByIdAsync(Guid cardId, CancellationToken ct = default)
        => await context.RetroCards
            .Include(c => c.Session)
            .Include(c => c.Votes)
            .FirstOrDefaultAsync(c => c.Id == cardId, ct);

    public async Task<RetroCard> AddCardAsync(RetroCard card, CancellationToken ct = default)
    {
        context.RetroCards.Add(card);
        await context.SaveChangesAsync(ct);
        return card;
    }

    public async Task<RetroCard> UpdateCardAsync(RetroCard card, CancellationToken ct = default)
    {
        context.RetroCards.Update(card);
        await context.SaveChangesAsync(ct);
        return card;
    }

    public async Task<RetroVote?> GetVoteAsync(Guid cardId, Guid voterId, CancellationToken ct = default)
        => await context.RetroVotes
            .FirstOrDefaultAsync(v => v.CardId == cardId && v.VoterId == voterId, ct);

    public async Task<RetroVote> AddVoteAsync(RetroVote vote, CancellationToken ct = default)
    {
        context.RetroVotes.Add(vote);
        await context.SaveChangesAsync(ct);
        return vote;
    }

    public async Task<RetroVote> UpdateVoteAsync(RetroVote vote, CancellationToken ct = default)
    {
        context.RetroVotes.Update(vote);
        await context.SaveChangesAsync(ct);
        return vote;
    }

    public async Task<int> GetTotalVoteCountForUserInSessionAsync(
        Guid sessionId, Guid voterId, CancellationToken ct = default)
        => await context.RetroVotes
            .Where(v => v.Card!.SessionId == sessionId && v.VoterId == voterId)
            .SumAsync(v => (int)v.VoteCount, ct);

    public async Task<RetroActionItem> AddActionItemAsync(RetroActionItem actionItem, CancellationToken ct = default)
    {
        context.RetroActionItems.Add(actionItem);
        await context.SaveChangesAsync(ct);
        return actionItem;
    }

    public async Task<RetroActionItem> UpdateActionItemAsync(RetroActionItem actionItem, CancellationToken ct = default)
    {
        context.RetroActionItems.Update(actionItem);
        await context.SaveChangesAsync(ct);
        return actionItem;
    }

    public async Task<IEnumerable<RetroActionItem>> GetActionItemsBySessionAsync(
        Guid sessionId, CancellationToken ct = default)
        => await context.RetroActionItems
            .AsNoTracking()
            .Include(a => a.Assignee)
            .Where(a => a.SessionId == sessionId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);
}
