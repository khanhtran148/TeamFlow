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

        // Get blocked status for all items
        var itemsList = items.ToList();
        var blockedItemIds = await GetBlockedItemIdsAsync(itemsList.Select(i => i.Id).ToList(), ct);

        var dtos = itemsList.Select(item => MapToDto(item, blockedItemIds));

        return Result.Success(new PagedResult<BacklogItemDto>(dtos, totalCount, request.Page, request.PageSize));
    }

    private async Task<HashSet<Guid>> GetBlockedItemIdsAsync(List<Guid> itemIds, CancellationToken ct)
    {
        var blockedIds = new HashSet<Guid>();
        // Batch check: for each item, check if it has unresolved blockers
        // For performance, we do a single query per batch
        foreach (var id in itemIds)
        {
            var blockers = await linkRepository.GetBlockersForItemAsync(id, ct);
            if (blockers.Any(l => l.Source is not null && l.Source.Status != Domain.Enums.WorkItemStatus.Done))
                blockedIds.Add(id);
        }
        return blockedIds;
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
