using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.WorkItems.GetHistory;

namespace TeamFlow.Application.Common.Interfaces;

public interface IWorkItemHistoryRepository
{
    Task<PagedResult<WorkItemHistoryDto>> GetByWorkItemAsync(
        Guid workItemId, int page, int pageSize, CancellationToken ct = default);
}
