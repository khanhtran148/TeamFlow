using System.Text.Json;
using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Sprints.UpdateCapacity;

public sealed class UpdateCapacityHandler(
    ISprintRepository sprintRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<UpdateCapacityCommand, Result>
{
    public async Task<Result> Handle(UpdateCapacityCommand request, CancellationToken ct)
    {
        var sprint = await sprintRepository.GetByIdAsync(request.SprintId, ct);
        if (sprint is null)
            return Result.Failure("Sprint not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, sprint.ProjectId, Permission.Sprint_Edit, ct))
            return Result.Failure("Access denied");

        if (sprint.Status is not SprintStatus.Planning)
            return Result.Failure("Capacity can only be updated for sprints in Planning status");

        var capacityDict = request.Capacity.ToDictionary(
            e => e.MemberId.ToString(),
            e => e.Points);

        sprint.CapacityJson = JsonDocument.Parse(JsonSerializer.Serialize(capacityDict));
        await sprintRepository.UpdateAsync(sprint, ct);

        return Result.Success();
    }
}
