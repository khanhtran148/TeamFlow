using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface ITeamHealthSummaryRepository
{
    Task AddAsync(TeamHealthSummary summary, CancellationToken ct = default);
    Task<TeamHealthSummary?> GetLatestByProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<(IEnumerable<TeamHealthSummary> Items, int TotalCount)> ListByProjectAsync(
        Guid projectId, int page, int pageSize, CancellationToken ct = default);
}
