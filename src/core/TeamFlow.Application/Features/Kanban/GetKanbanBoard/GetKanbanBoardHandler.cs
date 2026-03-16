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

        var blockedItemIds = await linkRepository.GetBlockedItemIdsAsync(itemsList.Select(i => i.Id), ct);

        var columns = BuildColumns(itemsList, blockedItemIds);

        IEnumerable<KanbanSwimlaneDto>? swimlanes = null;
        if (!string.IsNullOrEmpty(request.Swimlane))
        {
            swimlanes = request.Swimlane.ToLowerInvariant() switch
            {
                "assignee" => BuildAssigneeSwimlanes(itemsList, blockedItemIds),
                "epic" => BuildEpicSwimlanes(itemsList, blockedItemIds),
                _ => null
            };
        }

        return Result.Success(new KanbanBoardDto(request.ProjectId, columns, swimlanes));
    }

    private static List<KanbanColumnDto> BuildColumns(List<WorkItem> items, HashSet<Guid> blockedIds)
    {
        return KanbanStatuses.Select(status =>
        {
            var statusItems = items
                .Where(i => i.Status == status)
                .Select(i => MapToKanbanItem(i, blockedIds))
                .ToList();
            return new KanbanColumnDto(status, statusItems.Count, statusItems);
        }).ToList();
    }

    private static List<KanbanSwimlaneDto> BuildAssigneeSwimlanes(List<WorkItem> items, HashSet<Guid> blockedIds)
    {
        var grouped = items.GroupBy(i => new { i.AssigneeId, Name = i.Assignee?.Name ?? "Unassigned" });

        return grouped
            .OrderBy(g => g.Key.Name)
            .Select(g => new KanbanSwimlaneDto(
                g.Key.AssigneeId?.ToString() ?? "unassigned",
                g.Key.Name,
                BuildColumns(g.ToList(), blockedIds)
            ))
            .ToList();
    }

    private static List<KanbanSwimlaneDto> BuildEpicSwimlanes(List<WorkItem> items, HashSet<Guid> blockedIds)
    {
        var grouped = items.GroupBy(i => new { i.ParentId, Title = i.Parent?.Title ?? "No Epic" });

        return grouped
            .OrderBy(g => g.Key.Title)
            .Select(g => new KanbanSwimlaneDto(
                g.Key.ParentId?.ToString() ?? "no-epic",
                g.Key.Title,
                BuildColumns(g.ToList(), blockedIds)
            ))
            .ToList();
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
            item.AssignedAt,
            item.ParentId,
            item.Parent?.Title,
            blockedIds.Contains(item.Id),
            item.ReleaseId
        );
}
