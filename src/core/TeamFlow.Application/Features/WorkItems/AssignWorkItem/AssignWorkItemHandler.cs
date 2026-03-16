using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.WorkItems.AssignWorkItem;

public sealed class AssignWorkItemHandler(
    IWorkItemRepository workItemRepository,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<AssignWorkItemCommand, Result>
{
    public async Task<Result> Handle(AssignWorkItemCommand request, CancellationToken ct)
    {
        var item = await workItemRepository.GetByIdAsync(request.WorkItemId, ct);
        if (item is null)
            return Result.Failure("Work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, item.ProjectId, Permission.WorkItem_AssignOther, ct))
            return Result.Failure("Access denied");

        if (item.Type == WorkItemType.Epic)
            return Result.Failure("Epic cannot have an assignee");

        if (!await workItemRepository.UserExistsAsync(request.AssigneeId, ct))
            return Result.Failure("User not found");

        var oldAssigneeId = item.AssigneeId;
        item.AssigneeId = request.AssigneeId;
        item.AssignedAt = DateTime.UtcNow;

        await historyService.RecordAsync(new WorkItemHistoryEntry(
            item.Id, currentUser.Id, "Assigned", "AssigneeId",
            oldAssigneeId?.ToString(), request.AssigneeId.ToString()), ct);

        await workItemRepository.UpdateAsync(item, ct);

        await publisher.Publish(new WorkItemAssignedDomainEvent(
            item.Id, item.ProjectId, oldAssigneeId, request.AssigneeId, currentUser.Id), ct);

        return Result.Success();
    }
}
