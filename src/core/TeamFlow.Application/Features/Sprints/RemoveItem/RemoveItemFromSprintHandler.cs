using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Sprints.RemoveItem;

public sealed class RemoveItemFromSprintHandler(
    ISprintRepository sprintRepository,
    IWorkItemRepository workItemRepository,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<RemoveItemFromSprintCommand, Result>
{
    public async Task<Result> Handle(RemoveItemFromSprintCommand request, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdAsync(request.SprintId, ct);
        if (sprint is null)
            return Result.Failure("Sprint not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, sprint.ProjectId, Permission.Sprint_Edit, ct))
            return Result.Failure("Access denied");

        var workItem = await workItemRepository.GetByIdAsync(request.WorkItemId, ct);
        if (workItem is null)
            return Result.Failure("Work item not found");

        if (workItem.SprintId != request.SprintId)
            return Result.Failure("Work item does not belong to this sprint");

        workItem.SprintId = null;
        await workItemRepository.UpdateAsync(workItem, ct);

        await historyService.RecordAsync(new WorkItemHistoryEntry(
            workItem.Id,
            currentUser.Id,
            "SprintUnassigned",
            "SprintId",
            sprint.Id.ToString(),
            null
        ), ct);

        await publisher.Publish(new SprintItemRemovedDomainEvent(
            sprint.Id,
            workItem.Id,
            sprint.ProjectId,
            currentUser.Id
        ), ct);

        return Result.Success();
    }
}
