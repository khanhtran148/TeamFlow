using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Common.Interfaces;

public interface IOrganizationMemberRepository
{
    Task<OrganizationMember> AddAsync(OrganizationMember member, CancellationToken ct = default);
    Task<OrgRole?> GetMemberRoleAsync(Guid organizationId, Guid userId, CancellationToken ct = default);
    Task<bool> IsMemberAsync(Guid organizationId, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<(Organization Org, OrgRole Role, DateTime JoinedAt)>> ListOrganizationsForUserAsync(
        Guid userId, CancellationToken ct = default);

    // Phase 6: Member management
    Task<IReadOnlyList<(OrganizationMember Member, User User)>> ListByOrgWithUsersAsync(
        Guid organizationId, CancellationToken ct = default);
    Task<OrganizationMember?> GetByOrgAndUserAsync(
        Guid organizationId, Guid userId, CancellationToken ct = default);
    Task<int> CountByRoleAsync(Guid organizationId, OrgRole role, CancellationToken ct = default);
    Task UpdateAsync(OrganizationMember member, CancellationToken ct = default);
    Task DeleteAsync(OrganizationMember member, CancellationToken ct = default);
}
