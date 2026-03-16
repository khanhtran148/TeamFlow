using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class WorkItemRepository(TeamFlowDbContext context) : IWorkItemRepository
{
    public async Task<WorkItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.WorkItems
            .Include(w => w.Assignee)
            .Include(w => w.Sprint)
            .Include(w => w.Release)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<IReadOnlyList<WorkItem>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return await context.WorkItems
            .Include(w => w.Assignee)
            .Include(w => w.Sprint)
            .Include(w => w.Release)
            .Where(w => idList.Contains(w.Id))
            .ToListAsync(ct);
    }

    public async Task<WorkItem?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await context.WorkItems
            .Include(w => w.Assignee)
            .Include(w => w.Sprint)
            .Include(w => w.Release)
            .Include(w => w.Parent)
            .Include(w => w.Children)
            .Include(w => w.SourceLinks)
            .Include(w => w.TargetLinks)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<IEnumerable<WorkItem>> GetByProjectAsync(Guid projectId, CancellationToken ct = default)
        => await context.WorkItems
            .AsNoTracking()
            .Where(w => w.ProjectId == projectId)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<WorkItem>> GetBySprintAsync(Guid sprintId, CancellationToken ct = default)
        => await context.WorkItems
            .AsNoTracking()
            .Where(w => w.SprintId == sprintId)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<WorkItem>> GetBacklogAsync(Guid projectId, CancellationToken ct = default)
        => await context.WorkItems
            .AsNoTracking()
            .Where(w => w.ProjectId == projectId && w.SprintId == null)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(ct);

    public async Task<WorkItem> AddAsync(WorkItem workItem, CancellationToken ct = default)
    {
        context.WorkItems.Add(workItem);
        await context.SaveChangesAsync(ct);
        return workItem;
    }

    public async Task<WorkItem> UpdateAsync(WorkItem workItem, CancellationToken ct = default)
    {
        context.WorkItems.Update(workItem);
        await context.SaveChangesAsync(ct);
        return workItem;
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await context.WorkItems.FindAsync([id], ct);
        if (item is not null)
        {
            item.DeletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
        }
    }

    public async Task<IEnumerable<Guid>> SoftDeleteCascadeAsync(Guid id, CancellationToken ct = default)
    {
        // Collect all descendants recursively, then soft-delete
        var allIds = new List<Guid>();
        await CollectDescendantsAsync(id, allIds, ct);

        if (allIds.Count == 0)
            return allIds;

        var now = DateTime.UtcNow;
        // Use IgnoreQueryFilters to find already-loaded items without filter
        await context.WorkItems
            .IgnoreQueryFilters()
            .Where(w => allIds.Contains(w.Id) && w.DeletedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(w => w.DeletedAt, now), ct);

        return allIds;
    }

    private async Task CollectDescendantsAsync(Guid parentId, List<Guid> result, CancellationToken ct)
    {
        result.Add(parentId);

        var childIds = await context.WorkItems
            .Where(w => w.ParentId == parentId)
            .Select(w => w.Id)
            .ToListAsync(ct);

        foreach (var childId in childIds)
            await CollectDescendantsAsync(childId, result, ct);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await context.WorkItems.AnyAsync(w => w.Id == id, ct);

    public async Task<IEnumerable<WorkItem>> GetChildrenAsync(Guid parentId, CancellationToken ct = default)
        => await context.WorkItems
            .Where(w => w.ParentId == parentId)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<WorkItem>> GetAllDescendantsAsync(
        Guid parentId,
        CancellationToken ct = default)
    {
        var result = new List<WorkItem>();
        await CollectDescendantEntitiesAsync(parentId, result, ct);
        return result;
    }

    private async Task CollectDescendantEntitiesAsync(Guid parentId, List<WorkItem> result, CancellationToken ct)
    {
        var children = await context.WorkItems
            .Where(w => w.ParentId == parentId)
            .ToListAsync(ct);

        foreach (var child in children)
        {
            result.Add(child);
            await CollectDescendantEntitiesAsync(child.Id, result, ct);
        }
    }

    public async Task<(IEnumerable<WorkItem> Items, int TotalCount)> GetBacklogPagedAsync(
        Guid projectId,
        WorkItemStatus? status,
        Priority? priority,
        Guid? assigneeId,
        WorkItemType? type,
        Guid? sprintId,
        Guid? releaseId,
        bool? unscheduled,
        string? search,
        bool? isReady,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.WorkItems
            .AsNoTracking()
            .Include(w => w.Assignee)
            .Include(w => w.Release)
            .Where(w => w.ProjectId == projectId);

        if (status.HasValue)
            query = query.Where(w => w.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(w => w.Priority == priority.Value);

        if (assigneeId.HasValue)
            query = query.Where(w => w.AssigneeId == assigneeId.Value);

        if (type.HasValue)
            query = query.Where(w => w.Type == type.Value);

        if (sprintId.HasValue)
            query = query.Where(w => w.SprintId == sprintId.Value);

        if (releaseId.HasValue)
            query = query.Where(w => w.ReleaseId == releaseId.Value);

        if (unscheduled == true)
            query = query.Where(w => w.ReleaseId == null);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(w => w.Title.Contains(search));

        if (isReady.HasValue)
            query = query.Where(w => w.IsReadyForSprint == isReady.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(w => w.Type)
            .ThenBy(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IEnumerable<WorkItem>> GetByReleaseIdAsync(Guid releaseId, CancellationToken ct = default)
        => await context.WorkItems
            .AsNoTracking()
            .Include(w => w.Assignee)
            .Include(w => w.Release)
            .Include(w => w.Parent)
            .Include(w => w.Sprint)
            .Where(w => w.ReleaseId == releaseId)
            .OrderBy(w => w.Type)
            .ThenBy(w => w.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<WorkItem>> GetKanbanItemsAsync(
        Guid projectId,
        Guid? assigneeId,
        WorkItemType? type,
        Priority? priority,
        Guid? sprintId,
        Guid? releaseId,
        CancellationToken ct = default)
    {
        var query = context.WorkItems
            .AsNoTracking()
            .Include(w => w.Assignee)
            .Include(w => w.Release)
            .Include(w => w.Parent)
            .Where(w => w.ProjectId == projectId);

        if (assigneeId.HasValue)
            query = query.Where(w => w.AssigneeId == assigneeId.Value);

        if (type.HasValue)
            query = query.Where(w => w.Type == type.Value);

        if (priority.HasValue)
            query = query.Where(w => w.Priority == priority.Value);

        if (sprintId.HasValue)
            query = query.Where(w => w.SprintId == sprintId.Value);

        if (releaseId.HasValue)
            query = query.Where(w => w.ReleaseId == releaseId.Value);

        return await query
            .OrderBy(w => w.Status)
            .ThenBy(w => w.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task UpdateSortOrderAsync(Guid id, int sortOrder, CancellationToken ct = default)
    {
        await context.WorkItems
            .Where(w => w.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(w => w.SortOrder, sortOrder), ct);
    }

    public async Task<bool> UserExistsAsync(Guid userId, CancellationToken ct = default)
        => await context.Users.AnyAsync(u => u.Id == userId, ct);
}
