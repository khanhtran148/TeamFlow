using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Teams.CreateTeam;

public sealed class CreateTeamHandler(
    ITeamRepository teamRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<CreateTeamCommand, Result<TeamDto>>
{
    public async Task<Result<TeamDto>> Handle(CreateTeamCommand request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.OrgId, Permission.Team_Manage, ct))
            return Result.Failure<TeamDto>("Access denied");

        var team = new Team
        {
            OrgId = request.OrgId,
            Name = request.Name,
            Description = request.Description
        };

        await teamRepository.AddAsync(team, ct);

        return Result.Success(MapToDto(team));
    }

    private static TeamDto MapToDto(Team team) =>
        new(team.Id, team.OrgId, team.Name, team.Description, team.Members.Count, team.CreatedAt);
}
