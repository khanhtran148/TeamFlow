using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Releases.AssignItem;

public sealed class AssignItemToReleaseHandler(
    IReleaseRepository releaseRepository,
    IWorkItemRepository workItemRepository,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<AssignItemToReleaseCommand, Result>
{
    public async Task<Result> Handle(AssignItemToReleaseCommand request, CancellationToken ct)
    {
        var release = await releaseRepository.GetByIdAsync(request.ReleaseId, ct);
        if (release is null)
            return Result.Failure("Release not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, release.ProjectId, Permission.Release_Edit, ct))
            return Result.Failure("Access denied");

        var workItem = await workItemRepository.GetByIdAsync(request.WorkItemId, ct);
        if (workItem is null)
            return Result.Failure("Work item not found");

        if (workItem.ReleaseId.HasValue && workItem.ReleaseId.Value != request.ReleaseId)
            return Result.Failure("Work item is already assigned to another release. Unassign it first.");

        var oldReleaseId = workItem.ReleaseId;
        workItem.ReleaseId = request.ReleaseId;

        await historyService.RecordAsync(new WorkItemHistoryEntry(
            workItem.Id, currentUser.Id, "AssignedToRelease", "ReleaseId",
            oldReleaseId?.ToString(), request.ReleaseId.ToString()), ct);

        await workItemRepository.UpdateAsync(workItem, ct);

        await publisher.Publish(new ReleaseItemAssignedDomainEvent(
            request.ReleaseId, request.WorkItemId, release.ProjectId, currentUser.Id), ct);

        return Result.Success();
    }
}
