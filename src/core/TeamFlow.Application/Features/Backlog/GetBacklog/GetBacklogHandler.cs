using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Backlog.GetBacklog;

public sealed class GetBacklogHandler(
    IWorkItemRepository workItemRepository,
    IWorkItemLinkRepository linkRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<GetBacklogQuery, Result<PagedResult<BacklogItemDto>>>
{
    public async Task<Result<PagedResult<BacklogItemDto>>> Handle(GetBacklogQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.WorkItem_View, ct))
            return Result.Failure<PagedResult<BacklogItemDto>>("Access denied");

        var (items, totalCount) = await workItemRepository.GetBacklogPagedAsync(
            request.ProjectId,
            request.Status,
            request.Priority,
            request.AssigneeId,
            request.Type,
            request.SprintId,
            request.ReleaseId,
            request.Unscheduled,
            request.Search,
            request.Page,
            request.PageSize,
            ct);

        // Get blocked status for all items using a single batch query (no N+1)
        var itemsList = items.ToList();
        var blockedItemIds = await linkRepository.GetBlockedItemIdsAsync(itemsList.Select(i => i.Id), ct);

        var dtos = itemsList.Select(item => MapToDto(item, blockedItemIds));

        return Result.Success(new PagedResult<BacklogItemDto>(dtos, totalCount, request.Page, request.PageSize));
    }

    private static BacklogItemDto MapToDto(WorkItem item, HashSet<Guid> blockedIds) =>
        new(
            item.Id,
            item.ProjectId,
            item.ParentId,
            item.Type,
            item.Title,
            item.Status,
            item.Priority,
            item.AssigneeId,
            item.Assignee?.Name,
            item.ReleaseId,
            item.Release?.Name,
            blockedIds.Contains(item.Id),
            item.SortOrder,
            []  // Children loaded separately for hierarchy view
        );
}
