using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class ProjectRepository(TeamFlowDbContext context) : IProjectRepository
{
    public async Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Projects
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Project> AddAsync(Project project, CancellationToken ct = default)
    {
        context.Projects.Add(project);
        await context.SaveChangesAsync(ct);
        return project;
    }

    public async Task<Project> UpdateAsync(Project project, CancellationToken ct = default)
    {
        context.Projects.Update(project);
        await context.SaveChangesAsync(ct);
        return project;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await context.Projects.AnyAsync(p => p.Id == id, ct);

    public async Task<(IEnumerable<Project> Items, int TotalCount)> ListAsync(
        Guid? orgId,
        string? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Projects.AsNoTracking();

        if (orgId.HasValue)
            query = query.Where(p => p.OrgId == orgId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<int> CountWorkItemsAsync(Guid projectId, CancellationToken ct = default)
        => await context.WorkItems
            .CountAsync(w => w.ProjectId == projectId, ct);

    public async Task<int> CountOpenWorkItemsAsync(Guid projectId, CancellationToken ct = default)
        => await context.WorkItems
            .CountAsync(w => w.ProjectId == projectId
                && w.Status != WorkItemStatus.Done
                && w.Status != WorkItemStatus.Rejected, ct);

    public async Task<int> CountEpicsAsync(Guid projectId, CancellationToken ct = default)
        => await context.WorkItems
            .CountAsync(w => w.ProjectId == projectId && w.Type == WorkItemType.Epic, ct);
}
