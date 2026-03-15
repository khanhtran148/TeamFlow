using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Sprints.CreateSprint;

public sealed class CreateSprintHandler(
    ISprintRepository sprintRepository,
    IProjectRepository projectRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<CreateSprintCommand, Result<SprintDto>>
{
    public async Task<Result<SprintDto>> Handle(CreateSprintCommand request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Sprint_Create, ct))
            return Result.Failure<SprintDto>("Access denied");

        if (!await projectRepository.ExistsAsync(request.ProjectId, ct))
            return Result.Failure<SprintDto>("Project not found");

        var sprint = new Sprint
        {
            ProjectId = request.ProjectId,
            Name = request.Name,
            Goal = request.Goal,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        await sprintRepository.AddAsync(sprint, ct);

        return Result.Success(SprintMapper.ToDto(sprint));
    }
}
