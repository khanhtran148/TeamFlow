using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class ProjectMembershipRepository(TeamFlowDbContext context) : IProjectMembershipRepository
{
    public async Task<ProjectMembership?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.ProjectMemberships
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<IEnumerable<ProjectMembership>> GetByProjectAsync(Guid projectId, CancellationToken ct = default)
        => await context.ProjectMemberships
            .AsNoTracking()
            .Where(m => m.ProjectId == projectId)
            .ToListAsync(ct);

    public async Task<ProjectMembership> AddAsync(ProjectMembership membership, CancellationToken ct = default)
    {
        context.ProjectMemberships.Add(membership);
        await context.SaveChangesAsync(ct);
        return membership;
    }

    public async Task DeleteAsync(ProjectMembership membership, CancellationToken ct = default)
    {
        context.ProjectMemberships.Remove(membership);
        await context.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(Guid projectId, Guid memberId, string memberType, CancellationToken ct = default)
        => await context.ProjectMemberships
            .AnyAsync(m => m.ProjectId == projectId
                && m.MemberId == memberId
                && m.MemberType == memberType, ct);
}
