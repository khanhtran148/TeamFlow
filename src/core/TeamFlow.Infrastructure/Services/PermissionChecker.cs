using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Authorization;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Services;

/// <summary>
/// HUMAN-REVIEWED: This class implements the 3-level permission resolution.
/// Resolution order: Individual → Team → Organization.
/// See docs/product/roles-permissions.md for the full matrix.
/// </summary>
public sealed class PermissionChecker(TeamFlowDbContext context) : IPermissionChecker
{
    public async Task<bool> HasPermissionAsync(
        Guid userId, Guid projectId, Permission permission, CancellationToken ct = default)
    {
        // Org_Admin permission: the "projectId" parameter may actually be an OrgId
        // (e.g., CreateProject passes OrgId since no project exists yet).
        if (permission == Permission.Org_Admin)
            return await IsOrgAdminAsync(userId, projectId, ct);

        var effectiveRole = await GetEffectiveRoleAsync(userId, projectId, ct);
        if (effectiveRole == ProjectRole.OrgAdmin)
            return true;

        if (effectiveRole is null)
            return false;

        return PermissionMatrix.RoleHasPermission(effectiveRole.Value, permission);
    }

    public async Task<ProjectRole?> GetEffectiveRoleAsync(
        Guid userId, Guid projectId, CancellationToken ct = default)
    {
        var projectOrgId = await context.Projects
            .AsNoTracking()
            .Where(p => p.Id == projectId)
            .Select(p => (Guid?)p.OrgId)
            .FirstOrDefaultAsync(ct);

        if (projectOrgId is null)
            return null;

        // Check org admin first — pass resolved orgId directly to avoid redundant lookup
        if (await IsOrgAdminByOrgIdAsync(userId, projectOrgId.Value, ct))
            return ProjectRole.OrgAdmin;

        // Fetch individual + team memberships for this project in one query
        var userTeamIds = context.TeamMembers
            .AsNoTracking()
            .Where(tm => tm.UserId == userId)
            .Select(tm => tm.TeamId);

        var memberships = await context.ProjectMemberships
            .AsNoTracking()
            .Where(pm =>
                (pm.ProjectId == projectId && pm.MemberType == "User" && pm.MemberId == userId) ||
                (pm.ProjectId == projectId && pm.MemberType == "Team" && userTeamIds.Contains(pm.MemberId)))
            .Select(pm => new { pm.MemberType, pm.MemberId, pm.Role })
            .ToListAsync(ct);

        // Individual override takes precedence
        var individual = memberships.FirstOrDefault(m =>
            m.MemberType == "User" && m.MemberId == userId);
        if (individual is not null)
            return individual.Role;

        // Team membership
        var teamRole = memberships.FirstOrDefault(m => m.MemberType == "Team");
        if (teamRole is not null)
            return teamRole.Role;

        return null;
    }

    /// <summary>
    /// Checks if the user is an OrgAdmin via OrganizationMember table.
    /// The orgOrProjectId can be either an OrgId or a ProjectId — tries org first, falls back to project lookup.
    /// </summary>
    private async Task<bool> IsOrgAdminAsync(
        Guid userId, Guid orgOrProjectId, CancellationToken ct)
    {
        // Try as OrgId first (single query combining org check + membership)
        var isAdminByOrg = await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m =>
                m.OrganizationId == orgOrProjectId &&
                m.UserId == userId &&
                (m.Role == OrgRole.Owner || m.Role == OrgRole.Admin), ct);

        if (isAdminByOrg)
            return true;

        // If no match, it might be a ProjectId — resolve to OrgId
        var projectOrgId = await context.Projects
            .AsNoTracking()
            .Where(p => p.Id == orgOrProjectId)
            .Select(p => (Guid?)p.OrgId)
            .FirstOrDefaultAsync(ct);

        if (projectOrgId is null)
            return false;

        return await IsOrgAdminByOrgIdAsync(userId, projectOrgId.Value, ct);
    }

    /// <summary>
    /// Checks org admin when the OrgId is already known (avoids redundant lookups).
    /// </summary>
    private async Task<bool> IsOrgAdminByOrgIdAsync(
        Guid userId, Guid orgId, CancellationToken ct)
    {
        return await context.OrganizationMembers
            .AsNoTracking()
            .AnyAsync(m =>
                m.OrganizationId == orgId &&
                m.UserId == userId &&
                (m.Role == OrgRole.Owner || m.Role == OrgRole.Admin), ct);
    }
}
