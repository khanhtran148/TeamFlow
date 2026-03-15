using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Common.Interfaces;

public interface IWorkItemLinkRepository
{
    Task<WorkItemLink?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<WorkItemLink?> FindAsync(Guid sourceId, Guid targetId, LinkType linkType, CancellationToken ct = default);
    Task<IEnumerable<WorkItemLink>> GetLinksForItemAsync(Guid workItemId, CancellationToken ct = default);
    Task<WorkItemLink> AddAsync(WorkItemLink link, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<WorkItemLink> links, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task DeletePairAsync(Guid sourceId, Guid targetId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid sourceId, Guid targetId, LinkType linkType, CancellationToken ct = default);
    /// <summary>
    /// Returns all target IDs reachable from sourceId via the given link type (for circular detection).
    /// </summary>
    Task<IEnumerable<Guid>> GetReachableTargetsAsync(Guid sourceId, LinkType linkType, CancellationToken ct = default);
    Task<IEnumerable<WorkItemLink>> GetBlockersForItemAsync(Guid workItemId, CancellationToken ct = default);
    /// <summary>
    /// Returns the set of item IDs (from the given list) that are currently blocked
    /// by an active blocker (i.e. a Blocks-link whose source is not Done).
    /// Issues a single batch query instead of N per-item queries.
    /// </summary>
    Task<HashSet<Guid>> GetBlockedItemIdsAsync(IEnumerable<Guid> itemIds, CancellationToken ct = default);
}
