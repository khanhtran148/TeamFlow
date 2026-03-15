using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class ReleaseRepository(TeamFlowDbContext context) : IReleaseRepository
{
    public async Task<Release?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Releases
            .Include(r => r.WorkItems)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Release> AddAsync(Release release, CancellationToken ct = default)
    {
        context.Releases.Add(release);
        await context.SaveChangesAsync(ct);
        return release;
    }

    public async Task<Release> UpdateAsync(Release release, CancellationToken ct = default)
    {
        context.Releases.Update(release);
        await context.SaveChangesAsync(ct);
        return release;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var release = await context.Releases.FindAsync([id], ct);
        if (release is not null)
        {
            context.Releases.Remove(release);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await context.Releases.AnyAsync(r => r.Id == id, ct);

    public async Task<(IEnumerable<Release> Items, int TotalCount)> ListByProjectAsync(
        Guid projectId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Releases
            .AsNoTracking()
            .Where(r => r.ProjectId == projectId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Dictionary<WorkItemStatus, int>> GetItemStatusCountsAsync(
        Guid releaseId,
        CancellationToken ct = default)
    {
        var counts = await context.WorkItems
            .AsNoTracking()
            .Where(w => w.ReleaseId == releaseId)
            .GroupBy(w => w.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return counts.ToDictionary(x => x.Status, x => x.Count);
    }

    public async Task UnlinkAllItemsAsync(Guid releaseId, CancellationToken ct = default)
    {
        await context.WorkItems
            .Where(w => w.ReleaseId == releaseId)
            .ExecuteUpdateAsync(s => s.SetProperty(w => w.ReleaseId, (Guid?)null), ct);
    }
}
