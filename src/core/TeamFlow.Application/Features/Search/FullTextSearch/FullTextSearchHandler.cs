using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.WorkItems;

namespace TeamFlow.Application.Features.Search.FullTextSearch;

public sealed class FullTextSearchHandler(
    IWorkItemRepository workItemRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<FullTextSearchQuery, Result<PagedResult<WorkItemDto>>>
{
    public async Task<Result<PagedResult<WorkItemDto>>> Handle(
        FullTextSearchQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.WorkItem_View, ct))
            return Result.Failure<PagedResult<WorkItemDto>>("Access denied");

        var (items, totalCount) = await workItemRepository.GetBacklogPagedAsync(
            request.ProjectId,
            request.Status?.Length == 1 ? request.Status[0] : null,
            request.Priority?.Length == 1 ? request.Priority[0] : null,
            request.AssigneeId,
            request.Type?.Length == 1 ? request.Type[0] : null,
            request.SprintId,
            request.ReleaseId,
            unscheduled: null,
            search: request.Q,
            isReady: null,
            request.Page,
            request.PageSize,
            ct);

        const int defaultLinkCount = 0;
        const int defaultChildCount = 0;

        var dtos = items.Select(item => new WorkItemDto(
            item.Id,
            item.ProjectId,
            item.ParentId,
            item.Type,
            item.Title,
            item.Description,
            item.Status,
            item.Priority,
            item.EstimationValue,
            item.AssigneeId,
            item.Assignee?.Name,
            item.SprintId,
            item.ReleaseId,
            defaultLinkCount,
            defaultChildCount,
            item.SortOrder,
            item.CreatedAt,
            item.UpdatedAt
        )).ToList();

        return Result.Success(new PagedResult<WorkItemDto>(dtos, totalCount, request.Page, request.PageSize));
    }
}
