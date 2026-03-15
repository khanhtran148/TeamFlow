using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface ISprintSnapshotRepository
{
    Task<SprintSnapshot> AddAsync(SprintSnapshot snapshot, CancellationToken ct = default);
    Task<SprintSnapshot?> GetFinalAsync(Guid sprintId, CancellationToken ct = default);
}
