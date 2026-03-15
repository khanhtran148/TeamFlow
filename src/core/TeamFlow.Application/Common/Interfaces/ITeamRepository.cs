using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Team?> GetByIdWithMembersAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<Team> Items, int TotalCount)> ListByOrgAsync(Guid orgId, int page, int pageSize, CancellationToken ct = default);
    Task<Team> AddAsync(Team team, CancellationToken ct = default);
    Task<Team> UpdateAsync(Team team, CancellationToken ct = default);
    Task DeleteAsync(Team team, CancellationToken ct = default);
}
