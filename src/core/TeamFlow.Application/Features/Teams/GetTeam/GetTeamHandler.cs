using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Teams.GetTeam;

public sealed class GetTeamHandler(ITeamRepository teamRepository)
    : IRequestHandler<GetTeamQuery, Result<TeamDto>>
{
    public async Task<Result<TeamDto>> Handle(GetTeamQuery request, CancellationToken ct)
    {
        var team = await teamRepository.GetByIdWithMembersAsync(request.TeamId, ct);
        if (team is null)
            return Result.Failure<TeamDto>("Team not found");

        return Result.Success(MapToDto(team));
    }

    private static TeamDto MapToDto(Team team) =>
        new(team.Id, team.OrgId, team.Name, team.Description, team.Members.Count, team.CreatedAt);
}
