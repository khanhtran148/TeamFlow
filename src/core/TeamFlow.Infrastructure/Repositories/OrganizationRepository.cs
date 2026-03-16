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

    public async Task<Organization?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => await context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Slug == slug, ct);

    public async Task<bool> ExistsBySlugAsync(string slug, CancellationToken ct = default)
        => await context.Organizations
            .AsNoTracking()
            .AnyAsync(o => o.Slug == slug, ct);

    public async Task<Organization> AddAsync(Organization organization, CancellationToken ct = default)
    {
        context.Organizations.Add(organization);
        await context.SaveChangesAsync(ct);
        return organization;
    }

    public async Task<Organization> UpdateAsync(Organization organization, CancellationToken ct = default)
    {
        context.Organizations.Update(organization);
        await context.SaveChangesAsync(ct);
        return organization;
    }

    public async Task<IEnumerable<Organization>> ListByUserAsync(Guid userId, CancellationToken ct = default)
    {
        // Orgs where the user is a member (via OrganizationMember)
        var memberOrgIds = context.OrganizationMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.OrganizationId);

        return await context.Organizations
            .AsNoTracking()
            .Where(o => memberOrgIds.Contains(o.Id))
            .OrderBy(o => o.Name)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Organization>> ListAllAsync(CancellationToken ct = default)
        => await context.Organizations
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .ToListAsync(ct);
}
