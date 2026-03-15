using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class SprintRepository(TeamFlowDbContext context) : ISprintRepository
{
    public async Task<Sprint?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Sprints
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Sprint?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default)
        => await context.Sprints
            .Include(s => s.WorkItems)
                .ThenInclude(w => w.Assignee)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Sprint?> GetActiveSprintForProjectAsync(Guid projectId, CancellationToken ct = default)
        => await context.Sprints
            .FirstOrDefaultAsync(s => s.ProjectId == projectId && s.Status == SprintStatus.Active, ct);

    public async Task<(IEnumerable<Sprint> Items, int TotalCount)> ListByProjectPagedAsync(
        Guid projectId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.Sprints
            .AsNoTracking()
            .Where(s => s.ProjectId == projectId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Include(s => s.WorkItems)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Sprint> AddAsync(Sprint sprint, CancellationToken ct = default)
    {
        context.Sprints.Add(sprint);
        await context.SaveChangesAsync(ct);
        return sprint;
    }

    public async Task<Sprint> UpdateAsync(Sprint sprint, CancellationToken ct = default)
    {
        context.Sprints.Update(sprint);
        await context.SaveChangesAsync(ct);
        return sprint;
    }

    public async Task DeleteAsync(Sprint sprint, CancellationToken ct = default)
    {
        context.Sprints.Remove(sprint);
        await context.SaveChangesAsync(ct);
    }
}
