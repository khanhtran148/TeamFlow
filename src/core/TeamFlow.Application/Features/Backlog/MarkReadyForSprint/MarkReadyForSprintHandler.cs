using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Backlog.MarkReadyForSprint;

public sealed class MarkReadyForSprintHandler(
    IWorkItemRepository workItemRepo,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<MarkReadyForSprintCommand, Result>
{
    public async Task<Result> Handle(MarkReadyForSprintCommand request, CancellationToken ct)
    {
        var workItem = await workItemRepo.GetByIdAsync(request.WorkItemId, ct);
        if (workItem is null)
            return DomainError.NotFound("Work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, workItem.ProjectId, Permission.WorkItem_Edit, ct))
            return DomainError.Forbidden();

        var oldValue = workItem.IsReadyForSprint.ToString();
        workItem.IsReadyForSprint = request.IsReady;
        await workItemRepo.UpdateAsync(workItem, ct);

        await historyService.RecordAsync(new WorkItemHistoryEntry(
            workItem.Id,
            currentUser.Id,
            "FieldChanged",
            "IsReadyForSprint",
            oldValue,
            request.IsReady.ToString()
        ), ct);

        return Result.Success();
    }
}
