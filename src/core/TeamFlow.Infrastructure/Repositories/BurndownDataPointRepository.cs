using Microsoft.EntityFrameworkCore;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Infrastructure.Persistence;

namespace TeamFlow.Infrastructure.Repositories;

public sealed class BurndownDataPointRepository(TeamFlowDbContext context) : IBurndownDataPointRepository
{
    public async Task<IEnumerable<BurndownDataPoint>> GetBySprintAsync(Guid sprintId, CancellationToken ct = default)
        => await context.BurndownDataPoints
            .AsNoTracking()
            .Where(b => b.SprintId == sprintId)
            .OrderBy(b => b.RecordedDate)
            .ToListAsync(ct);

    public async Task<BurndownDataPoint> AddAsync(BurndownDataPoint point, CancellationToken ct = default)
    {
        context.BurndownDataPoints.Add(point);
        await context.SaveChangesAsync(ct);
        return point;
    }

    public async Task<bool> ExistsForDateAsync(Guid sprintId, DateOnly date, CancellationToken ct = default)
        => await context.BurndownDataPoints
            .AnyAsync(b => b.SprintId == sprintId && b.RecordedDate == date, ct);
}
