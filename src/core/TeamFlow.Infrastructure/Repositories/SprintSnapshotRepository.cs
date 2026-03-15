using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class SprintSnapshotRepository(TeamFlowDbContext context) : ISprintSnapshotRepository
{
    public async Task<SprintSnapshot> AddAsync(SprintSnapshot snapshot, CancellationToken ct = default)
    {
        context.SprintSnapshots.Add(snapshot);
        await context.SaveChangesAsync(ct);
        return snapshot;
    }

    public async Task<SprintSnapshot?> GetFinalAsync(Guid sprintId, CancellationToken ct = default)
        => await context.SprintSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SprintId == sprintId && s.IsFinal, ct);
}
