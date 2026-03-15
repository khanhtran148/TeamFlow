using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.WorkItems.GetHistory;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class WorkItemHistoryRepository(TeamFlowDbContext context) : IWorkItemHistoryRepository
{
    public async Task<PagedResult<WorkItemHistoryDto>> GetByWorkItemAsync(
        Guid workItemId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.WorkItemHistories
            .AsNoTracking()
            .Where(h => h.WorkItemId == workItemId)
            .OrderByDescending(h => h.CreatedAt);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new WorkItemHistoryDto(
                h.Id,
                h.ActorId,
                h.Actor != null ? h.Actor.Name : null,
                h.ActorType,
                h.ActionType,
                h.FieldName,
                h.OldValue,
                h.NewValue,
                h.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<WorkItemHistoryDto>(items, totalCount, page, pageSize);
    }
}
