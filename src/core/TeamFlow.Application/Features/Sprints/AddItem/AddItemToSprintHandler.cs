using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Sprints.AddItem;

public sealed class AddItemToSprintHandler(
    ISprintRepository sprintRepository,
    IWorkItemRepository workItemRepository,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<AddItemToSprintCommand, Result>
{
    public async Task<Result> Handle(AddItemToSprintCommand request, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdAsync(request.SprintId, ct);
        if (sprint is null)
            return Result.Failure("Sprint not found");

        if (!sprint.CanAddItem())
            return Result.Failure("Cannot add items to a Completed sprint");

        // Active sprint requires elevated permission (Sprint_Start acts as Sprint_AddItem)
        var requiredPermission = sprint.Status is SprintStatus.Active
            ? Permission.Sprint_Start
            : Permission.Sprint_Edit;

        if (!await permissions.HasPermissionAsync(currentUser.Id, sprint.ProjectId, requiredPermission, ct))
            return Result.Failure("Access denied");

        var workItem = await workItemRepository.GetByIdAsync(request.WorkItemId, ct);
        if (workItem is null)
            return Result.Failure("Work item not found");

        if (workItem.SprintId.HasValue && workItem.SprintId != request.SprintId)
            return Result.Failure("Work item already belongs to another sprint. Conflict detected.");

        workItem.SprintId = request.SprintId;
        await workItemRepository.UpdateAsync(workItem, ct);

        await historyService.RecordAsync(new WorkItemHistoryEntry(
            workItem.Id,
            currentUser.Id,
            "SprintAssigned",
            "SprintId",
            null,
            sprint.Id.ToString()
        ), ct);

        await publisher.Publish(new SprintItemAddedDomainEvent(
            sprint.Id,
            workItem.Id,
            sprint.ProjectId,
            currentUser.Id
        ), ct);

        return Result.Success();
    }
}
