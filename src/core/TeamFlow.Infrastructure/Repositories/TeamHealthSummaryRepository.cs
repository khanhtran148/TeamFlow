using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class TeamHealthSummaryRepository(TeamFlowDbContext context) : ITeamHealthSummaryRepository
{
    public async Task AddAsync(TeamHealthSummary summary, CancellationToken ct = default)
    {
        context.TeamHealthSummaries.Add(summary);
        await context.SaveChangesAsync(ct);
    }

    public async Task<TeamHealthSummary?> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default)
        => await context.TeamHealthSummaries
            .AsNoTracking()
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.PeriodEnd)
            .FirstOrDefaultAsync(ct);

    public async Task<(IEnumerable<TeamHealthSummary> Items, int TotalCount)> ListByProjectAsync(
        Guid projectId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.TeamHealthSummaries
            .AsNoTracking()
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.PeriodEnd);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }
}
