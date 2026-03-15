using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.WorkItems.CreateWorkItem;

public sealed class CreateWorkItemHandler(
    IWorkItemRepository workItemRepository,
    IProjectRepository projectRepository,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<CreateWorkItemCommand, Result<WorkItemDto>>
{
    public async Task<Result<WorkItemDto>> Handle(CreateWorkItemCommand request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.WorkItem_Create, ct))
            return Result.Failure<WorkItemDto>("Access denied");

        if (!await projectRepository.ExistsAsync(request.ProjectId, ct))
            return Result.Failure<WorkItemDto>("Project not found");

        // Validate hierarchy rules
        var hierarchyError = await ValidateHierarchyAsync(request, ct);
        if (hierarchyError is not null)
            return Result.Failure<WorkItemDto>(hierarchyError);

        var item = new WorkItem
        {
            ProjectId = request.ProjectId,
            ParentId = request.ParentId,
            Type = request.Type,
            Title = request.Title,
            Description = request.Description,
            Status = WorkItemStatus.ToDo,
            Priority = request.Priority
        };

        await workItemRepository.AddAsync(item, ct);

        await historyService.RecordAsync(new WorkItemHistoryEntry(
            item.Id,
            currentUser.Id,
            "Created",
            null,
            null,
            item.Title
        ), ct);

        await publisher.Publish(new WorkItemCreatedDomainEvent(
            item.Id,
            item.ProjectId,
            item.Type,
            item.Title,
            currentUser.Id
        ), ct);

        return Result.Success(MapToDto(item));
    }

    private async Task<string?> ValidateHierarchyAsync(CreateWorkItemCommand request, CancellationToken ct)
    {
        return request.Type switch
        {
            WorkItemType.Epic when request.ParentId.HasValue =>
                "Epic cannot have a parent",

            WorkItemType.UserStory when !request.ParentId.HasValue =>
                null, // Stories CAN have no parent (top-level story in backlog)

            WorkItemType.UserStory when request.ParentId.HasValue =>
                await ValidateParentTypeAsync(request.ParentId.Value, WorkItemType.Epic, "UserStory parent must be an Epic", ct),

            WorkItemType.Task or WorkItemType.Bug or WorkItemType.Spike when !request.ParentId.HasValue =>
                null, // Allow no parent

            WorkItemType.Task or WorkItemType.Bug or WorkItemType.Spike when request.ParentId.HasValue =>
                await ValidateParentTypeAsync(request.ParentId.Value, WorkItemType.UserStory, $"{request.Type} must have a UserStory parent", ct),

            _ => null
        };
    }

    private async Task<string?> ValidateParentTypeAsync(
        Guid parentId,
        WorkItemType requiredType,
        string errorMessage,
        CancellationToken ct)
    {
        var parent = await workItemRepository.GetByIdAsync(parentId, ct);
        if (parent is null)
            return "Parent work item not found";

        return parent.Type != requiredType ? errorMessage : null;
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
            0,
            0,
            item.SortOrder,
            item.CreatedAt,
            item.UpdatedAt
        );
}
