using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface ISavedFilterRepository
{
    Task<SavedFilter?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SavedFilter>> ListByUserAndProjectAsync(Guid userId, Guid projectId, CancellationToken ct = default);
    Task<SavedFilter> AddAsync(SavedFilter filter, CancellationToken ct = default);
    Task<SavedFilter> UpdateAsync(SavedFilter filter, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(Guid userId, Guid projectId, string name, CancellationToken ct = default);
}
