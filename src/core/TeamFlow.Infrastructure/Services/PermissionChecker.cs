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

        // Check org admin first
        if (await IsOrgAdminAsync(userId, projectOrgId.Value, ct))
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
    /// Checks if the user is an OrgAdmin. The orgOrProjectId can be either an OrgId
    /// (for org-level operations like CreateProject) or resolved from a project.
    /// </summary>
    private async Task<bool> IsOrgAdminAsync(
        Guid userId, Guid orgOrProjectId, CancellationToken ct)
    {
        // Determine if this is an OrgId or ProjectId
        var orgId = orgOrProjectId;
        var isOrg = await context.Organizations
            .AsNoTracking()
            .AnyAsync(o => o.Id == orgId, ct);

        if (!isOrg)
        {
            // Maybe it's a ProjectId — resolve the OrgId
            var projectOrgId = await context.Projects
                .AsNoTracking()
                .Where(p => p.Id == orgOrProjectId)
                .Select(p => (Guid?)p.OrgId)
                .FirstOrDefaultAsync(ct);

            if (projectOrgId is null)
                return false;

            orgId = projectOrgId.Value;
        }

        // Check if user has OrgAdmin role on any project in this org
        var hasAdminMembership = await context.ProjectMemberships
            .AsNoTracking()
            .AnyAsync(pm =>
                pm.MemberType == "User" &&
                pm.MemberId == userId &&
                pm.Role == ProjectRole.OrgAdmin &&
                pm.Project!.OrgId == orgId, ct);

        if (hasAdminMembership)
            return true;

        // Bootstrap: if NO OrgAdmin exists for this org at all,
        // allow the operation so the first user can create the first project.
        // Once an OrgAdmin membership is created, this path is never taken again.
        var anyAdminExists = await context.ProjectMemberships
            .AsNoTracking()
            .AnyAsync(pm =>
                pm.MemberType == "User" &&
                pm.Role == ProjectRole.OrgAdmin &&
                pm.Project!.OrgId == orgId, ct);

        return !anyAdminExists;
    }
}
