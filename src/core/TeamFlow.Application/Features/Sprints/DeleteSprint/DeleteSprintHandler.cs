using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Sprints.DeleteSprint;

public sealed class DeleteSprintHandler(
    ISprintRepository sprintRepository,
    IWorkItemRepository workItemRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<DeleteSprintCommand, Result>
{
    public async Task<Result> Handle(DeleteSprintCommand request, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdWithItemsAsync(request.SprintId, ct);
        if (sprint is null)
            return Result.Failure("Sprint not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, sprint.ProjectId, Permission.Sprint_Edit, ct))
            return Result.Failure("Access denied");

        if (sprint.Status is not SprintStatus.Planning)
            return Result.Failure("Cannot delete a sprint that is not in Planning status");

        // Unlink all work items from this sprint (snapshot to avoid modifying tracked collection during iteration)
        foreach (var item in sprint.WorkItems.ToList())
        {
            item.SprintId = null;
            await workItemRepository.UpdateAsync(item, ct);
        }

        await sprintRepository.DeleteAsync(sprint, ct);

        return Result.Success();
    }
}
