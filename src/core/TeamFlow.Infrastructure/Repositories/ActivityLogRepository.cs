using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Users;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class ActivityLogRepository(TeamFlowDbContext context) : IActivityLogRepository
{
    public async Task<(IReadOnlyList<ActivityLogItemDto> Items, int TotalCount)> GetPagedByUserAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.WorkItemHistories
            .AsNoTracking()
            .Where(h => h.ActorId == userId && h.ActorType == "User")
            .Join(
                context.WorkItems,
                h => h.WorkItemId,
                w => w.Id,
                (h, w) => new { History = h, WorkItemTitle = w.Title });

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.History.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ActivityLogItemDto(
                x.History.Id,
                x.History.WorkItemId,
                x.WorkItemTitle,
                x.History.ActionType,
                x.History.FieldName,
                x.History.OldValue,
                x.History.NewValue,
                x.History.CreatedAt))
            .ToListAsync(ct);

        return (items.AsReadOnly(), totalCount);
    }
}
