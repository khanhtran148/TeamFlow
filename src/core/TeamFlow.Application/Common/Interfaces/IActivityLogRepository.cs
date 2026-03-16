using TeamFlow.Application.Features.Users;

namespace TeamFlow.Application.Common.Interfaces;

public interface IActivityLogRepository
{
    Task<(IReadOnlyList<ActivityLogItemDto> Items, int TotalCount)> GetPagedByUserAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default);
}
