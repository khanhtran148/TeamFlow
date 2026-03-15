using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Teams;

namespace TeamFlow.Application.Features.Teams.UpdateTeam;

public sealed record UpdateTeamCommand(
    Guid TeamId,
    string Name,
    string? Description
) : IRequest<Result<TeamDto>>;
