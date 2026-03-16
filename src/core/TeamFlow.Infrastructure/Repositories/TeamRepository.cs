using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class TeamRepository(TeamFlowDbContext context) : ITeamRepository
{
    public async Task<Team?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Teams
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Team?> GetByIdWithMembersAsync(Guid id, CancellationToken ct = default)
        => await context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<(IEnumerable<Team> Items, int TotalCount)> ListByOrgAsync(
        Guid orgId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.Teams
            .AsNoTracking()
            .Include(t => t.Members)
            .Where(t => t.OrgId == orgId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(t => t.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<Team> AddAsync(Team team, CancellationToken ct = default)
    {
        context.Teams.Add(team);
        await context.SaveChangesAsync(ct);
        return team;
    }

    public async Task<Team> UpdateAsync(Team team, CancellationToken ct = default)
    {
        if (context.Entry(team).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            context.Teams.Update(team);
        await context.SaveChangesAsync(ct);
        return team;
    }

    public async Task DeleteAsync(Team team, CancellationToken ct = default)
    {
        context.Teams.Remove(team);
        await context.SaveChangesAsync(ct);
    }
}
