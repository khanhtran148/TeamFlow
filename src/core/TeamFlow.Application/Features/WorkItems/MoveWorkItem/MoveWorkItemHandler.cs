using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.WorkItems.MoveWorkItem;

public sealed class MoveWorkItemHandler(
    IWorkItemRepository workItemRepository,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<MoveWorkItemCommand, Result>
{
    public async Task<Result> Handle(MoveWorkItemCommand request, CancellationToken ct)
    {
        var item = await workItemRepository.GetByIdAsync(request.WorkItemId, ct);
        if (item is null)
            return Result.Failure("Work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, item.ProjectId, Permission.WorkItem_Edit, ct))
            return Result.Failure("Access denied");

        if (item.Type == WorkItemType.Epic)
            return Result.Failure("Epics cannot be reparented");

        if (request.NewParentId.HasValue)
        {
            var newParent = await workItemRepository.GetByIdAsync(request.NewParentId.Value, ct);
            if (newParent is null)
                return Result.Failure("New parent work item not found");

            var requiredParentType = item.Type == WorkItemType.UserStory
                ? WorkItemType.Epic
                : WorkItemType.UserStory;

            if (newParent.Type != requiredParentType)
                return Result.Failure($"{item.Type} must be placed under a {requiredParentType}");
        }

        var oldParentId = item.ParentId;
        item.ParentId = request.NewParentId;

        await historyService.RecordAsync(new WorkItemHistoryEntry(
            item.Id, currentUser.Id, "Moved", "ParentId",
            oldParentId?.ToString(), request.NewParentId?.ToString()), ct);

        await workItemRepository.UpdateAsync(item, ct);

        return Result.Success();
    }
}
