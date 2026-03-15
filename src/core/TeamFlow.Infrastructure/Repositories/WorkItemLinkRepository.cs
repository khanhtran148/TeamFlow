using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class WorkItemLinkRepository(TeamFlowDbContext context) : IWorkItemLinkRepository
{
    public async Task<WorkItemLink?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.WorkItemLinks
            .Include(l => l.Source)
            .Include(l => l.Target)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<WorkItemLink?> FindAsync(
        Guid sourceId,
        Guid targetId,
        LinkType linkType,
        CancellationToken ct = default)
        => await context.WorkItemLinks
            .FirstOrDefaultAsync(l =>
                l.SourceId == sourceId &&
                l.TargetId == targetId &&
                l.LinkType == linkType, ct);

    public async Task<IEnumerable<WorkItemLink>> GetLinksForItemAsync(
        Guid workItemId,
        CancellationToken ct = default)
        => await context.WorkItemLinks
            .AsNoTracking()
            .Include(l => l.Source)
            .Include(l => l.Target)
            .Where(l => l.SourceId == workItemId || l.TargetId == workItemId)
            .ToListAsync(ct);

    public async Task<WorkItemLink> AddAsync(WorkItemLink link, CancellationToken ct = default)
    {
        context.WorkItemLinks.Add(link);
        await context.SaveChangesAsync(ct);
        return link;
    }

    public async Task AddRangeAsync(IEnumerable<WorkItemLink> links, CancellationToken ct = default)
    {
        context.WorkItemLinks.AddRange(links);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var link = await context.WorkItemLinks.FindAsync([id], ct);
        if (link is not null)
        {
            context.WorkItemLinks.Remove(link);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task DeletePairAsync(Guid sourceId, Guid targetId, CancellationToken ct = default)
    {
        var links = await context.WorkItemLinks
            .Where(l =>
                (l.SourceId == sourceId && l.TargetId == targetId) ||
                (l.SourceId == targetId && l.TargetId == sourceId))
            .ToListAsync(ct);

        if (links.Count > 0)
        {
            context.WorkItemLinks.RemoveRange(links);
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> ExistsAsync(
        Guid sourceId,
        Guid targetId,
        LinkType linkType,
        CancellationToken ct = default)
        => await context.WorkItemLinks
            .AnyAsync(l =>
                l.SourceId == sourceId &&
                l.TargetId == targetId &&
                l.LinkType == linkType, ct);

    public async Task<IEnumerable<Guid>> GetReachableTargetsAsync(
        Guid sourceId,
        LinkType linkType,
        CancellationToken ct = default)
    {
        // BFS to find all reachable nodes from sourceId via linkType
        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(sourceId);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
                continue;

            var targets = await context.WorkItemLinks
                .AsNoTracking()
                .Where(l => l.SourceId == current && l.LinkType == linkType)
                .Select(l => l.TargetId)
                .ToListAsync(ct);

            foreach (var target in targets)
            {
                if (!visited.Contains(target))
                    queue.Enqueue(target);
            }
        }

        // Remove the starting node itself
        visited.Remove(sourceId);
        return visited;
    }

    public async Task<IEnumerable<WorkItemLink>> GetBlockersForItemAsync(
        Guid workItemId,
        CancellationToken ct = default)
    {
        // A blocker for workItemId is a link where workItemId is the target
        // and the link type is Blocks (meaning: source Blocks workItemId)
        return await context.WorkItemLinks
            .AsNoTracking()
            .Include(l => l.Source)
            .Where(l => l.TargetId == workItemId && l.LinkType == LinkType.Blocks)
            .ToListAsync(ct);
    }
}
