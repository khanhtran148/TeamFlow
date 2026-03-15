using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Teams;

namespace TeamFlow.Application.Features.Teams.CreateTeam;

public sealed record CreateTeamCommand(
    Guid OrgId,
    string Name,
    string? Description
) : IRequest<Result<TeamDto>>;
