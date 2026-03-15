using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Teams.DeleteTeam;

public sealed class DeleteTeamHandler(
    ITeamRepository teamRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<DeleteTeamCommand, Result>
{
    public async Task<Result> Handle(DeleteTeamCommand request, CancellationToken ct)
    {
        var team = await teamRepository.GetByIdAsync(request.TeamId, ct);
        if (team is null)
            return Result.Failure("Team not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, team.OrgId, Permission.Team_Manage, ct))
            return Result.Failure("Access denied");

        await teamRepository.DeleteAsync(team, ct);

        return Result.Success();
    }
}
