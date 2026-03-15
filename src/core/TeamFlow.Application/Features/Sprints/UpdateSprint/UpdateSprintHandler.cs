using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Sprints.UpdateSprint;

public sealed class UpdateSprintHandler(
    ISprintRepository sprintRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<UpdateSprintCommand, Result<SprintDto>>
{
    public async Task<Result<SprintDto>> Handle(UpdateSprintCommand request, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdWithItemsAsync(request.SprintId, ct);
        if (sprint is null)
            return Result.Failure<SprintDto>("Sprint not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, sprint.ProjectId, Permission.Sprint_Edit, ct))
            return Result.Failure<SprintDto>("Access denied");

        if (sprint.Status is not SprintStatus.Planning)
            return Result.Failure<SprintDto>("Cannot update a sprint that is not in Planning status");

        sprint.Name = request.Name;
        sprint.Goal = request.Goal;
        sprint.StartDate = request.StartDate;
        sprint.EndDate = request.EndDate;

        await sprintRepository.UpdateAsync(sprint, ct);

        return Result.Success(SprintMapper.ToDto(sprint));
    }
}
