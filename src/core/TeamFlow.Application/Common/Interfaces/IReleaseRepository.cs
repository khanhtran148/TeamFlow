using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Common.Interfaces;

public interface IReleaseRepository
{
    Task<Release?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Release> AddAsync(Release release, CancellationToken ct = default);
    Task<Release> UpdateAsync(Release release, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<Release> Items, int TotalCount)> ListByProjectAsync(
        Guid projectId,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<Dictionary<WorkItemStatus, int>> GetItemStatusCountsAsync(Guid releaseId, CancellationToken ct = default);
    Task UnlinkAllItemsAsync(Guid releaseId, CancellationToken ct = default);
}
