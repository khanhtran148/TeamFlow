using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Project> AddAsync(Project project, CancellationToken ct = default);
    Task<Project> UpdateAsync(Project project, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<Project> Items, int TotalCount)> ListAsync(
        Guid? orgId,
        string? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<int> CountWorkItemsAsync(Guid projectId, CancellationToken ct = default);
    Task<int> CountOpenWorkItemsAsync(Guid projectId, CancellationToken ct = default);
    Task<int> CountEpicsAsync(Guid projectId, CancellationToken ct = default);
}
