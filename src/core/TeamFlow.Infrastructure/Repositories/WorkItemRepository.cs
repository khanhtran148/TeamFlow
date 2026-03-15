using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public class WorkItemRepository : IWorkItemRepository
{
    private readonly TeamFlowDbContext _context;

    public WorkItemRepository(TeamFlowDbContext context)
    {
        _context = context;
    }

    public async Task<WorkItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.WorkItems
            .Include(w => w.Assignee)
            .Include(w => w.Sprint)
            .Include(w => w.Release)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<IEnumerable<WorkItem>> GetByProjectAsync(Guid projectId, CancellationToken ct = default)
        => await _context.WorkItems
            .Where(w => w.ProjectId == projectId)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<WorkItem>> GetBySprintAsync(Guid sprintId, CancellationToken ct = default)
        => await _context.WorkItems
            .Where(w => w.SprintId == sprintId)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<WorkItem>> GetBacklogAsync(Guid projectId, CancellationToken ct = default)
        => await _context.WorkItems
            .Where(w => w.ProjectId == projectId && w.SprintId == null)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(ct);

    public async Task<WorkItem> AddAsync(WorkItem workItem, CancellationToken ct = default)
    {
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync(ct);
        return workItem;
    }

    public async Task<WorkItem> UpdateAsync(WorkItem workItem, CancellationToken ct = default)
    {
        _context.WorkItems.Update(workItem);
        await _context.SaveChangesAsync(ct);
        return workItem;
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _context.WorkItems.FindAsync([id], ct);
        if (item is not null)
        {
            item.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _context.WorkItems.AnyAsync(w => w.Id == id, ct);

    public async Task<IEnumerable<WorkItem>> GetChildrenAsync(Guid parentId, CancellationToken ct = default)
        => await _context.WorkItems
            .Where(w => w.ParentId == parentId)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(ct);
}
