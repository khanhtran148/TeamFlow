using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class SprintReportRepository(TeamFlowDbContext context) : ISprintReportRepository
{
    public async Task AddAsync(SprintReport report, CancellationToken ct = default)
    {
        context.SprintReports.Add(report);
        await context.SaveChangesAsync(ct);
    }

    public async Task<SprintReport?> GetBySprintIdAsync(Guid sprintId, CancellationToken ct = default)
        => await context.SprintReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.SprintId == sprintId, ct);

    public async Task<(IEnumerable<SprintReport> Items, int TotalCount)> ListByProjectAsync(
        Guid projectId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.SprintReports
            .AsNoTracking()
            .Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.GeneratedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }
}
