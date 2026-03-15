using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Teams;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Teams.ListTeams;

public sealed class ListTeamsHandler(ITeamRepository teamRepository)
    : IRequestHandler<ListTeamsQuery, Result<PagedResult<TeamDto>>>
{
    public async Task<Result<PagedResult<TeamDto>>> Handle(ListTeamsQuery request, CancellationToken ct)
    {
        var (items, totalCount) = await teamRepository.ListByOrgAsync(request.OrgId, request.Page, request.PageSize, ct);

        var dtos = items.Select(MapToDto);

        return Result.Success(new PagedResult<TeamDto>(dtos, totalCount, request.Page, request.PageSize));
    }

    private static TeamDto MapToDto(Team team) =>
        new(team.Id, team.OrgId, team.Name, team.Description, team.Members.Count, team.CreatedAt);
}
