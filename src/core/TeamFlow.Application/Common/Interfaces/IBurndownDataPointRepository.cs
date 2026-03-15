using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Common.Interfaces;

public interface IBurndownDataPointRepository
{
    Task<IEnumerable<BurndownDataPoint>> GetBySprintAsync(Guid sprintId, CancellationToken ct = default);
    Task<BurndownDataPoint> AddAsync(BurndownDataPoint point, CancellationToken ct = default);
    Task<bool> ExistsForDateAsync(Guid sprintId, DateOnly date, CancellationToken ct = default);
}
