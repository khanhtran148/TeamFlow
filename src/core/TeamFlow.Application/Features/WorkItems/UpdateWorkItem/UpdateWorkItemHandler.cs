using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.WorkItems.UpdateWorkItem;

public sealed class UpdateWorkItemHandler(
    IWorkItemRepository workItemRepository,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<UpdateWorkItemCommand, Result<WorkItemDto>>
{
    public async Task<Result<WorkItemDto>> Handle(UpdateWorkItemCommand request, CancellationToken ct)
    {
        var item = await workItemRepository.GetByIdAsync(request.WorkItemId, ct);
        if (item is null)
            return Result.Failure<WorkItemDto>("Work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, item.ProjectId, Permission.WorkItem_Edit, ct))
            return Result.Failure<WorkItemDto>("Access denied");

        // Track and record changes
        if (item.Title != request.Title)
        {
            await historyService.RecordAsync(new WorkItemHistoryEntry(
                item.Id, currentUser.Id, "Updated", "Title", item.Title, request.Title), ct);
            item.Title = request.Title;
        }

        if (item.Description != request.Description)
        {
            await historyService.RecordAsync(new WorkItemHistoryEntry(
                item.Id, currentUser.Id, "Updated", "Description", item.Description, request.Description), ct);
            item.Description = request.Description;
        }

        if (item.Priority != request.Priority)
        {
            await publisher.Publish(new WorkItemPriorityChangedDomainEvent(
                item.Id, item.ProjectId, item.Priority, request.Priority, currentUser.Id), ct);
            await historyService.RecordAsync(new WorkItemHistoryEntry(
                item.Id, currentUser.Id, "Updated", "Priority",
                item.Priority?.ToString(), request.Priority?.ToString()), ct);
            item.Priority = request.Priority;
        }

        if (item.EstimationValue != request.EstimationValue)
        {
            await publisher.Publish(new WorkItemEstimationChangedDomainEvent(
                item.Id, item.ProjectId, item.EstimationValue, request.EstimationValue, "Human", currentUser.Id), ct);
            await historyService.RecordAsync(new WorkItemHistoryEntry(
                item.Id, currentUser.Id, "Updated", "EstimationValue",
                item.EstimationValue?.ToString(), request.EstimationValue?.ToString()), ct);
            item.EstimationValue = request.EstimationValue;
        }

        await workItemRepository.UpdateAsync(item, ct);

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
