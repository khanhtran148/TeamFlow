using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class OrganizationMemberRepository(TeamFlowDbContext context) : IOrganizationMemberRepository
{
    public async Task<OrganizationMember> AddAsync(OrganizationMember member, CancellationToken ct = default)
    {
        context.OrganizationMembers.Add(member);
        await context.SaveChangesAsync(ct);
        return member;
    }

    public async Task<OrgRole?> GetMemberRoleAsync(Guid organizationId, Guid userId, CancellationToken ct = default)
    {
        var member = await context.OrganizationMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId, ct);

        return member?.Role;
    }

    public async Task<bool> IsMemberAsync(Guid organizationId, Guid userId, CancellationToken ct = default)
        => await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m => m.OrganizationId == organizationId && m.UserId == userId, ct);

    public async Task<IEnumerable<(Organization Org, OrgRole Role, DateTime JoinedAt)>> ListOrganizationsForUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        var memberships = await context.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.Organization)
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Organization!.Name)
            .ToListAsync(ct);

        return memberships
            .Where(m => m.Organization is not null)
            .Select(m => (m.Organization!, m.Role, m.JoinedAt));
    }

    public async Task<IReadOnlyList<(OrganizationMember Member, User User)>> ListByOrgWithUsersAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        var members = await context.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.OrganizationId == organizationId)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(ct);

        return members
            .Where(m => m.User is not null)
            .Select(m => (m, m.User!))
            .ToList()
            .AsReadOnly();
    }

    public async Task<OrganizationMember?> GetByOrgAndUserAsync(
        Guid organizationId, Guid userId, CancellationToken ct = default)
        => await context.OrganizationMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.UserId == userId, ct);

    public async Task<int> CountByRoleAsync(Guid organizationId, OrgRole role, CancellationToken ct = default)
        => await context.OrganizationMembers
            .CountAsync(m => m.OrganizationId == organizationId && m.Role == role, ct);

    public async Task UpdateAsync(OrganizationMember member, CancellationToken ct = default)
    {
        context.OrganizationMembers.Update(member);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(OrganizationMember member, CancellationToken ct = default)
    {
        context.OrganizationMembers.Remove(member);
        await context.SaveChangesAsync(ct);
    }
}
