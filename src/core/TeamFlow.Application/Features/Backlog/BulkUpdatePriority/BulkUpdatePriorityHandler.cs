using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Backlog.BulkUpdatePriority;

public sealed class BulkUpdatePriorityHandler(
    IWorkItemRepository workItemRepo,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<BulkUpdatePriorityCommand, Result>
{
    public async Task<Result> Handle(BulkUpdatePriorityCommand request, CancellationToken ct)
    {
        var ids = request.Items.Select(i => i.WorkItemId).ToList();
        var workItems = await workItemRepo.GetByIdsAsync(ids, ct);
        var workItemMap = workItems.ToDictionary(w => w.Id);

        foreach (var id in ids)
        {
            if (!workItemMap.ContainsKey(id))
                return DomainError.NotFound($"Work item {id} not found");
        }

        foreach (var item in request.Items)
        {
            var workItem = workItemMap[item.WorkItemId];

            if (!await permissions.HasPermissionAsync(currentUser.Id, workItem.ProjectId, Permission.WorkItem_Edit, ct))
                return DomainError.Forbidden($"Access denied for work item {item.WorkItemId}");

            var oldValue = workItem.Priority?.ToString();
            workItem.Priority = item.Priority;
            await workItemRepo.UpdateAsync(workItem, ct);

            await historyService.RecordAsync(new WorkItemHistoryEntry(
                workItem.Id,
                currentUser.Id,
                "FieldChanged",
                "Priority",
                oldValue,
                item.Priority.ToString()
            ), ct);
        }

        return Result.Success();
    }
}
