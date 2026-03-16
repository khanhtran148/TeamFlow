using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Releases.UnassignItem;

public sealed class UnassignItemFromReleaseHandler(
    IReleaseRepository releaseRepository,
    IWorkItemRepository workItemRepository,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<UnassignItemFromReleaseCommand, Result>
{
    public async Task<Result> Handle(UnassignItemFromReleaseCommand request, CancellationToken ct)
    {
        var release = await releaseRepository.GetByIdAsync(request.ReleaseId, ct);
        if (release is null)
            return DomainError.NotFound("Release not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, release.ProjectId, Permission.Release_Edit, ct))
            return DomainError.Forbidden();

        var workItem = await workItemRepository.GetByIdAsync(request.WorkItemId, ct);
        if (workItem is null)
            return DomainError.NotFound("Work item not found");

        if (workItem.ReleaseId != request.ReleaseId)
            return DomainError.Validation("Work item is not assigned to this release");

        workItem.ReleaseId = null;

        await historyService.RecordAsync(new WorkItemHistoryEntry(
            workItem.Id, currentUser.Id, "UnassignedFromRelease", "ReleaseId",
            request.ReleaseId.ToString(), null), ct);

        await workItemRepository.UpdateAsync(workItem, ct);

        return Result.Success();
    }
}
