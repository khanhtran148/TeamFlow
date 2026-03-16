using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;

namespace TeamFlow.Application.Features.Users.GetActivityLog;

public sealed class GetActivityLogHandler(
    IActivityLogRepository activityLogRepository,
    ICurrentUser currentUser)
    : IRequestHandler<GetActivityLogQuery, Result<PagedResult<ActivityLogItemDto>>>
{
    private const int MaxPageSize = 50;

    public async Task<Result<PagedResult<ActivityLogItemDto>>> Handle(
        GetActivityLogQuery request, CancellationToken ct)
    {
        var pageSize = Math.Min(request.PageSize, MaxPageSize);

        var (items, totalCount) = await activityLogRepository.GetPagedByUserAsync(
            currentUser.Id, request.Page, pageSize, ct);

        return Result.Success(new PagedResult<ActivityLogItemDto>(items, totalCount, request.Page, pageSize));
    }
}
