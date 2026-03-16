using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class SavedFilterRepository(TeamFlowDbContext context) : ISavedFilterRepository
{
    public async Task<SavedFilter?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.SavedFilters.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<SavedFilter>> ListByUserAndProjectAsync(
        Guid userId, Guid projectId, CancellationToken ct = default)
        => await context.SavedFilters
            .AsNoTracking()
            .Where(f => f.UserId == userId && f.ProjectId == projectId)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);

    public async Task<SavedFilter> AddAsync(SavedFilter filter, CancellationToken ct = default)
    {
        context.SavedFilters.Add(filter);
        await context.SaveChangesAsync(ct);
        return filter;
    }

    public async Task<SavedFilter> UpdateAsync(SavedFilter filter, CancellationToken ct = default)
    {
        context.SavedFilters.Update(filter);
        await context.SaveChangesAsync(ct);
        return filter;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var filter = await context.SavedFilters.FindAsync([id], ct);
        if (filter is not null)
        {
            context.SavedFilters.Remove(filter);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> ExistsByNameAsync(
        Guid userId, Guid projectId, string name, CancellationToken ct = default)
        => await context.SavedFilters
            .AnyAsync(f => f.UserId == userId && f.ProjectId == projectId && f.Name == name, ct);
}
