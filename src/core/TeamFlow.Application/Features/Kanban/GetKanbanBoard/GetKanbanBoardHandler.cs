using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Kanban.GetKanbanBoard;

public sealed class GetKanbanBoardHandler(
    IWorkItemRepository workItemRepository,
    IWorkItemLinkRepository linkRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<GetKanbanBoardQuery, Result<KanbanBoardDto>>
{
    private static readonly WorkItemStatus[] KanbanStatuses =
        [WorkItemStatus.ToDo, WorkItemStatus.InProgress, WorkItemStatus.InReview, WorkItemStatus.Done];

    public async Task<Result<KanbanBoardDto>> Handle(GetKanbanBoardQuery request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.WorkItem_View, ct))
            return Result.Failure<KanbanBoardDto>("Access denied");

        var items = await workItemRepository.GetKanbanItemsAsync(
            request.ProjectId,
            request.AssigneeId,
            request.Type,
            request.Priority,
            request.SprintId,
            request.ReleaseId,
            ct);

        var itemsList = items.ToList();

        // Get blocked status using a single batch query (no N+1)
        var blockedItemIds = await linkRepository.GetBlockedItemIdsAsync(itemsList.Select(i => i.Id), ct);

        var columns = KanbanStatuses.Select(status =>
        {
            var statusItems = itemsList
                .Where(i => i.Status == status)
                .Select(i => MapToKanbanItem(i, blockedItemIds))
                .ToList();

            return new KanbanColumnDto(status, statusItems.Count, statusItems);
        }).ToList();

        return Result.Success(new KanbanBoardDto(request.ProjectId, columns));
    }

    private static KanbanItemDto MapToKanbanItem(WorkItem item, HashSet<Guid> blockedIds) =>
        new(
            item.Id,
            item.Type,
            item.Title,
            item.Status,
            item.Priority,
            item.AssigneeId,
            item.Assignee?.Name,
            item.ParentId,
            item.Parent?.Title,
            blockedIds.Contains(item.Id),
            item.ReleaseId
        );
}
