using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Sprints.StartSprint;

public sealed class StartSprintHandler(
    ISprintRepository sprintRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<StartSprintCommand, Result<SprintDto>>
{
    public async Task<Result<SprintDto>> Handle(StartSprintCommand request, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdWithItemsAsync(request.SprintId, ct);
        if (sprint is null)
            return Result.Failure<SprintDto>("Sprint not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, sprint.ProjectId, Permission.Sprint_Start, ct))
            return Result.Failure<SprintDto>("Access denied");

        // Check no other active sprint exists for this project
        var activeSprint = await sprintRepository.GetActiveSprintForProjectAsync(sprint.ProjectId, ct);
        if (activeSprint is not null)
            return Result.Failure<SprintDto>("Another sprint is already active for this project. Conflict detected.");

        var startResult = sprint.Start();
        if (startResult.IsFailure)
            return Result.Failure<SprintDto>(startResult.Error);

        await sprintRepository.UpdateAsync(sprint, ct);

        await publisher.Publish(new SprintStartedDomainEvent(
            sprint.Id,
            sprint.ProjectId,
            sprint.Name,
            sprint.Goal,
            sprint.StartDate!.Value,
            sprint.EndDate!.Value,
            currentUser.Id
        ), ct);

        return Result.Success(SprintMapper.ToDto(sprint));
    }
}
