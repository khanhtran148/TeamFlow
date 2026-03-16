using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface IRetroSessionRepository
{
    Task<RetroSession?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<RetroSession?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<RetroSession> AddAsync(RetroSession session, CancellationToken ct = default);
    Task<RetroSession> UpdateAsync(RetroSession session, CancellationToken ct = default);
    Task<(IEnumerable<RetroSession> Items, int TotalCount)> ListByProjectAsync(
        Guid projectId,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<RetroSession?> GetLastClosedByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<RetroCard?> GetCardByIdAsync(Guid cardId, CancellationToken ct = default);
    Task<RetroCard> AddCardAsync(RetroCard card, CancellationToken ct = default);
    Task<RetroCard> UpdateCardAsync(RetroCard card, CancellationToken ct = default);
    Task<RetroVote?> GetVoteAsync(Guid cardId, Guid voterId, CancellationToken ct = default);
    Task<RetroVote> AddVoteAsync(RetroVote vote, CancellationToken ct = default);
    Task<RetroVote> UpdateVoteAsync(RetroVote vote, CancellationToken ct = default);
    Task<int> GetTotalVoteCountForUserInSessionAsync(Guid sessionId, Guid voterId, CancellationToken ct = default);
    Task<RetroActionItem> AddActionItemAsync(RetroActionItem actionItem, CancellationToken ct = default);
    Task<IEnumerable<RetroActionItem>> GetActionItemsBySessionAsync(Guid sessionId, CancellationToken ct = default);
}
