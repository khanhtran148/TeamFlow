using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class TeamMemberRepository(TeamFlowDbContext context) : ITeamMemberRepository
{
    public async Task<IEnumerable<(Team Team, ProjectRole Role, DateTime JoinedAt)>> ListTeamsForUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        var memberships = await context.TeamMembers
            .AsNoTracking()
            .Include(tm => tm.Team)
                .ThenInclude(t => t!.Organization)
            .Where(tm => tm.UserId == userId)
            .OrderBy(tm => tm.Team!.Name)
            .ToListAsync(ct);

        return memberships
            .Where(tm => tm.Team is not null)
            .Select(tm => (tm.Team!, tm.Role, tm.JoinedAt));
    }
}
