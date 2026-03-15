using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface ISprintRepository
{
    Task<Sprint?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Sprint?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);
    Task<Sprint?> GetActiveSprintForProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<(IEnumerable<Sprint> Items, int TotalCount)> ListByProjectPagedAsync(
        Guid projectId, int page, int pageSize, CancellationToken ct = default);
    Task<Sprint> AddAsync(Sprint sprint, CancellationToken ct = default);
    Task<Sprint> UpdateAsync(Sprint sprint, CancellationToken ct = default);
    Task DeleteAsync(Sprint sprint, CancellationToken ct = default);
}
