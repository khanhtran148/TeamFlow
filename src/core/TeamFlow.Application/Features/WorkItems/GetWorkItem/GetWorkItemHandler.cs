using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.WorkItems.GetWorkItem;

public sealed class GetWorkItemHandler(
    IWorkItemRepository workItemRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<GetWorkItemQuery, Result<WorkItemDto>>
{
    public async Task<Result<WorkItemDto>> Handle(GetWorkItemQuery request, CancellationToken ct)
    {
        var item = await workItemRepository.GetByIdWithDetailsAsync(request.WorkItemId, ct);
        if (item is null)
            return Result.Failure<WorkItemDto>("Work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, item.ProjectId, Permission.WorkItem_View, ct))
            return Result.Failure<WorkItemDto>("Access denied");

        return Result.Success(MapToDto(item));
    }

    private static WorkItemDto MapToDto(WorkItem item) =>
        new(
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
            item.Children.Count,
            item.SourceLinks.Count + item.TargetLinks.Count,
            item.SortOrder,
            item.CreatedAt,
            item.UpdatedAt
        );
}
