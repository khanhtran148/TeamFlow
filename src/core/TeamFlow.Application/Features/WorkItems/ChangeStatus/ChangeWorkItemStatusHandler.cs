using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.WorkItems.ChangeStatus;

public sealed class ChangeWorkItemStatusHandler(
    IWorkItemRepository workItemRepository,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<ChangeWorkItemStatusCommand, Result<WorkItemDto>>
{
    // Valid transitions: from -> allowed tos
    private static readonly Dictionary<WorkItemStatus, HashSet<WorkItemStatus>> AllowedTransitions = new()
    {
        [WorkItemStatus.ToDo]       = [WorkItemStatus.InProgress],
        [WorkItemStatus.InProgress] = [WorkItemStatus.InReview, WorkItemStatus.ToDo],
        [WorkItemStatus.InReview]   = [WorkItemStatus.Done, WorkItemStatus.ToDo],
        [WorkItemStatus.Done]       = [WorkItemStatus.ToDo],
        [WorkItemStatus.NeedsClarification] = [WorkItemStatus.ToDo, WorkItemStatus.InProgress],
        [WorkItemStatus.Rejected]   = [WorkItemStatus.ToDo]
    };

    public async Task<Result<WorkItemDto>> Handle(ChangeWorkItemStatusCommand request, CancellationToken ct)
    {
        var item = await workItemRepository.GetByIdAsync(request.WorkItemId, ct);
        if (item is null)
            return Result.Failure<WorkItemDto>("Work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, item.ProjectId, Permission.WorkItem_ChangeStatus, ct))
            return Result.Failure<WorkItemDto>("Access denied");

        if (item.Status == request.NewStatus)
            return Result.Success(MapToDto(item));

        if (!AllowedTransitions.TryGetValue(item.Status, out var allowed) || !allowed.Contains(request.NewStatus))
            return Result.Failure<WorkItemDto>($"Invalid status transition from {item.Status} to {request.NewStatus}");

        var fromStatus = item.Status;
        item.Status = request.NewStatus;

        await historyService.RecordAsync(new WorkItemHistoryEntry(
            item.Id, currentUser.Id, "StatusChanged", "Status",
            fromStatus.ToString(), request.NewStatus.ToString()), ct);

        await workItemRepository.UpdateAsync(item, ct);

        await publisher.Publish(new WorkItemStatusChangedDomainEvent(
            item.Id, item.ProjectId, fromStatus, request.NewStatus, item.SprintId, currentUser.Id), ct);

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
            item.AssignedAt,
            item.SprintId,
            item.ReleaseId,
            item.Children.Count,
            item.SourceLinks.Count + item.TargetLinks.Count,
            item.SortOrder,
            item.CreatedAt,
            item.UpdatedAt
        );
}
