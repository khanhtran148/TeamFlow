using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Common.Interfaces;

public interface IWorkItemRepository
{
    Task<WorkItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<WorkItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<WorkItem>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<IEnumerable<WorkItem>> GetBySprintAsync(Guid sprintId, CancellationToken ct = default);
    Task<IEnumerable<WorkItem>> GetBacklogAsync(Guid projectId, CancellationToken ct = default);
    Task<WorkItem> AddAsync(WorkItem workItem, CancellationToken ct = default);
    Task<WorkItem> UpdateAsync(WorkItem workItem, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Guid>> SoftDeleteCascadeAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<WorkItem>> GetChildrenAsync(Guid parentId, CancellationToken ct = default);
    Task<IEnumerable<WorkItem>> GetAllDescendantsAsync(Guid parentId, CancellationToken ct = default);
    Task<(IEnumerable<WorkItem> Items, int TotalCount)> GetBacklogPagedAsync(
        Guid projectId,
        WorkItemStatus? status,
        Priority? priority,
        Guid? assigneeId,
        WorkItemType? type,
        Guid? sprintId,
        Guid? releaseId,
        bool? unscheduled,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<IEnumerable<WorkItem>> GetKanbanItemsAsync(
        Guid projectId,
        Guid? assigneeId,
        WorkItemType? type,
        Priority? priority,
        Guid? sprintId,
        Guid? releaseId,
        CancellationToken ct = default);
    Task UpdateSortOrderAsync(Guid id, int sortOrder, CancellationToken ct = default);
    Task<bool> UserExistsAsync(Guid userId, CancellationToken ct = default);
}
