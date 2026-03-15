using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Sprints.CompleteSprint;

public sealed class CompleteSprintHandler(
    ISprintRepository sprintRepository,
    IWorkItemRepository workItemRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<CompleteSprintCommand, Result<SprintDto>>
{
    public async Task<Result<SprintDto>> Handle(CompleteSprintCommand request, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdWithItemsAsync(request.SprintId, ct);
        if (sprint is null)
            return Result.Failure<SprintDto>("Sprint not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, sprint.ProjectId, Permission.Sprint_Complete, ct))
            return Result.Failure<SprintDto>("Access denied");

        var items = sprint.WorkItems?.ToList() ?? [];
        var plannedPoints = (int)items.Sum(w => w.EstimationValue ?? 0);
        var completedPoints = (int)items
            .Where(w => w.Status is WorkItemStatus.Done)
            .Sum(w => w.EstimationValue ?? 0);

        var completeResult = sprint.Complete();
        if (completeResult.IsFailure)
            return Result.Failure<SprintDto>(completeResult.Error);

        // Carry over incomplete items: unlink from sprint
        foreach (var itemId in completeResult.Value)
        {
            var item = await workItemRepository.GetByIdAsync(itemId, ct);
            if (item is not null)
            {
                item.SprintId = null;
                await workItemRepository.UpdateAsync(item, ct);
            }
        }

        await sprintRepository.UpdateAsync(sprint, ct);

        await publisher.Publish(new SprintCompletedDomainEvent(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            plannedPoints,
            completedPoints,
            currentUser.Id
        ), ct);

        return Result.Success(SprintMapper.ToDto(sprint));
    }
}
