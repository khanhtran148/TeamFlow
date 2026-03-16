using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface ISprintReportRepository
{
    Task AddAsync(SprintReport report, CancellationToken ct = default);
    Task<SprintReport?> GetBySprintIdAsync(Guid sprintId, CancellationToken ct = default);
    Task<(IEnumerable<SprintReport> Items, int TotalCount)> ListByProjectAsync(
        Guid projectId, int page, int pageSize, CancellationToken ct = default);
}
