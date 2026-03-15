using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.WorkItems.DeleteWorkItem;

public sealed class DeleteWorkItemHandler(
    IWorkItemRepository workItemRepository,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<DeleteWorkItemCommand, Result>
{
    public async Task<Result> Handle(DeleteWorkItemCommand request, CancellationToken ct)
    {
        var item = await workItemRepository.GetByIdAsync(request.WorkItemId, ct);
        if (item is null)
            return Result.Failure("Work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, item.ProjectId, Permission.WorkItem_Delete, ct))
            return Result.Failure("Access denied");

        var deletedIds = await workItemRepository.SoftDeleteCascadeAsync(request.WorkItemId, ct);

        foreach (var id in deletedIds)
        {
            await historyService.RecordAsync(new WorkItemHistoryEntry(
                id, currentUser.Id, "Deleted", null, null, null), ct);
        }

        return Result.Success();
    }
}
