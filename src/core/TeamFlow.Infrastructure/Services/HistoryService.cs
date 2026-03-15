using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Services;

public class HistoryService : IHistoryService
{
    private readonly TeamFlowDbContext _context;

    public HistoryService(TeamFlowDbContext context)
    {
        _context = context;
    }

    public async Task RecordAsync(WorkItemHistoryEntry entry, CancellationToken ct = default)
    {
        var history = new WorkItemHistory
        {
            WorkItemId = entry.WorkItemId,
            ActorId = entry.ActorId,
            ActorType = entry.ActorType,
            ActionType = entry.ActionType,
            FieldName = entry.FieldName,
            OldValue = entry.OldValue,
            NewValue = entry.NewValue
        };

        _context.WorkItemHistories.Add(history);
        await _context.SaveChangesAsync(ct);
    }
}
