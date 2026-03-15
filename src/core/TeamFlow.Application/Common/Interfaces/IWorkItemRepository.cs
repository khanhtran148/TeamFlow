using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Common.Interfaces;

public interface IWorkItemRepository
{
    Task<WorkItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<WorkItem>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<IEnumerable<WorkItem>> GetBySprintAsync(Guid sprintId, CancellationToken ct = default);
    Task<IEnumerable<WorkItem>> GetBacklogAsync(Guid projectId, CancellationToken ct = default);
    Task<WorkItem> AddAsync(WorkItem workItem, CancellationToken ct = default);
    Task<WorkItem> UpdateAsync(WorkItem workItem, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<WorkItem>> GetChildrenAsync(Guid parentId, CancellationToken ct = default);
}
