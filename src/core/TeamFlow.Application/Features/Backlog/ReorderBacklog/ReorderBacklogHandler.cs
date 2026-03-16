using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Backlog.ReorderBacklog;

public sealed class ReorderBacklogHandler(
    IWorkItemRepository workItemRepository,
    IPermissionChecker permissions,
    ICurrentUser currentUser)
    : IRequestHandler<ReorderBacklogCommand, Result>
{
    public async Task<Result> Handle(ReorderBacklogCommand request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.WorkItem_Edit, ct))
            return DomainError.Forbidden();

        foreach (var item in request.Items)
        {
            await workItemRepository.UpdateSortOrderAsync(item.WorkItemId, item.SortOrder, ct);
        }

        return Result.Success();
    }
}
