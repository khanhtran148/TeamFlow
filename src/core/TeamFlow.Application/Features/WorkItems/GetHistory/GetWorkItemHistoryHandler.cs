using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Features.WorkItems.GetHistory;

public sealed class GetWorkItemHistoryHandler(
    IWorkItemHistoryRepository historyRepository)
    : IRequestHandler<GetWorkItemHistoryQuery, Result<PagedResult<WorkItemHistoryDto>>>
{
    public async Task<Result<PagedResult<WorkItemHistoryDto>>> Handle(
        GetWorkItemHistoryQuery request, CancellationToken ct)
    {
        var result = await historyRepository.GetByWorkItemAsync(
            request.WorkItemId, request.Page, request.PageSize, ct);

        return Result.Success(result);
    }
}
