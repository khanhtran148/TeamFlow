using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class OrganizationRepository(TeamFlowDbContext context) : IOrganizationRepository
{
    public async Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<Organization> AddAsync(Organization organization, CancellationToken ct = default)
    {
        context.Organizations.Add(organization);
        await context.SaveChangesAsync(ct);
        return organization;
    }

    public async Task<IEnumerable<Organization>> ListByUserAsync(Guid userId, CancellationToken ct = default)
    {
        // Orgs where the user is the creator
        var createdOrgIds = context.Organizations
            .AsNoTracking()
            .Where(o => o.CreatedByUserId == userId)
            .Select(o => o.Id);

        // Orgs where the user has any project membership
        var memberOrgIds = context.ProjectMemberships
            .AsNoTracking()
            .Where(pm => pm.MemberId == userId && pm.MemberType == "User")
            .Join(context.Projects.AsNoTracking(),
                pm => pm.ProjectId,
                p => p.Id,
                (pm, p) => p.OrgId);

        var allOrgIds = createdOrgIds.Union(memberOrgIds).Distinct();

        return await context.Organizations
            .AsNoTracking()
            .Where(o => allOrgIds.Contains(o.Id))
            .OrderBy(o => o.Name)
            .ToListAsync(ct);
    }
}
