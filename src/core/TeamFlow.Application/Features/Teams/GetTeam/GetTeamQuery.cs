using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Teams;

namespace TeamFlow.Application.Features.Teams.GetTeam;

public sealed record GetTeamQuery(Guid TeamId) : IRequest<Result<TeamDto>>;
