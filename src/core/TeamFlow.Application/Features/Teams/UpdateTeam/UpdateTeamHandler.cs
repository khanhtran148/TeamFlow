using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Teams.UpdateTeam;

public sealed class UpdateTeamHandler(
    ITeamRepository teamRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<UpdateTeamCommand, Result<TeamDto>>
{
    public async Task<Result<TeamDto>> Handle(UpdateTeamCommand request, CancellationToken ct)
    {
        var team = await teamRepository.GetByIdWithMembersAsync(request.TeamId, ct);
        if (team is null)
            return Result.Failure<TeamDto>("Team not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, team.OrgId, Permission.Team_Manage, ct))
            return Result.Failure<TeamDto>("Access denied");

        team.Name = request.Name;
        team.Description = request.Description;

        await teamRepository.UpdateAsync(team, ct);

        return Result.Success(MapToDto(team));
    }

    private static TeamDto MapToDto(Team team) =>
        new(team.Id, team.OrgId, team.Name, team.Description, team.Members.Count, team.CreatedAt);
}
