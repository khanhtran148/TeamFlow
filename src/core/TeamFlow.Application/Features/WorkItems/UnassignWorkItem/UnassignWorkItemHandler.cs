using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.WorkItems.UnassignWorkItem;

public sealed class UnassignWorkItemHandler(
    IWorkItemRepository workItemRepository,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<UnassignWorkItemCommand, Result>
{
    public async Task<Result> Handle(UnassignWorkItemCommand request, CancellationToken ct)
    {
        var item = await workItemRepository.GetByIdAsync(request.WorkItemId, ct);
        if (item is null)
            return Result.Failure("Work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, item.ProjectId, Permission.WorkItem_AssignOther, ct))
            return Result.Failure("Access denied");

        if (item.AssigneeId.HasValue)
        {
            var previousAssigneeId = item.AssigneeId.Value;
            item.AssigneeId = null;

            await historyService.RecordAsync(new WorkItemHistoryEntry(
                item.Id, currentUser.Id, "Unassigned", "AssigneeId",
                previousAssigneeId.ToString(), null), ct);

            await publisher.Publish(new WorkItemUnassignedDomainEvent(
                item.Id, item.ProjectId, previousAssigneeId, currentUser.Id), ct);
        }

        await workItemRepository.UpdateAsync(item, ct);

        return Result.Success();
    }
}
