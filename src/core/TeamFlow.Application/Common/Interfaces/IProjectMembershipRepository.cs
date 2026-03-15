using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface IProjectMembershipRepository
{
    Task<ProjectMembership?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<ProjectMembership>> GetByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<ProjectMembership> AddAsync(ProjectMembership membership, CancellationToken ct = default);
    Task DeleteAsync(ProjectMembership membership, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid projectId, Guid memberId, string memberType, CancellationToken ct = default);
}
