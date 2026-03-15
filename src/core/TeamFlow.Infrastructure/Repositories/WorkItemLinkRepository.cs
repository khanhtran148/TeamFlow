using System.Runtime.CompilerServices;
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

    /// <summary>
    /// Returns all target IDs reachable from sourceId via the given link type using a single
    /// recursive CTE query with a depth limit of 100 to prevent infinite loops.
    /// </summary>
    public async Task<IEnumerable<Guid>> GetReachableTargetsAsync(
        Guid sourceId,
        LinkType linkType,
        CancellationToken ct = default)
    {
        var linkTypeString = linkType.ToString();

        // Recursive CTE: traverse the link graph in a single query.
        // Depth column guards against cycles (max 100 hops).
        var sql = """
            WITH RECURSIVE reachable(target_id, depth) AS (
                SELECT wil.target_id, 1
                FROM work_item_links wil
                WHERE wil.source_id = {0}
                  AND wil.link_type = {1}
              UNION
                SELECT wil.target_id, r.depth + 1
                FROM work_item_links wil
                JOIN reachable r ON wil.source_id = r.target_id
                WHERE wil.link_type = {1}
                  AND r.depth < 100
            )
            SELECT DISTINCT target_id AS "Value" FROM reachable
            """;

        var results = await context.Database
            .SqlQuery<Guid>(FormattableStringFactory.Create(sql, sourceId, linkTypeString))
            .ToListAsync(ct);

        return results;
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

    public async Task<HashSet<Guid>> GetBlockedItemIdsAsync(
        IEnumerable<Guid> itemIds,
        CancellationToken ct = default)
    {
        var ids = itemIds.ToList();
        if (ids.Count == 0)
            return [];

        // Single batch query: find all target IDs in the provided set
        // that have at least one active Blocks-link (source not Done).
        var blockedIds = await context.WorkItemLinks
            .AsNoTracking()
            .Include(l => l.Source)
            .Where(l =>
                l.LinkType == LinkType.Blocks &&
                ids.Contains(l.TargetId) &&
                l.Source != null &&
                l.Source.Status != WorkItemStatus.Done)
            .Select(l => l.TargetId)
            .Distinct()
            .ToListAsync(ct);

        return [.. blockedIds];
    }
}
