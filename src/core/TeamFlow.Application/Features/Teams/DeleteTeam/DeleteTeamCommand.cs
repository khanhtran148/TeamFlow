using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Teams.DeleteTeam;

public sealed record DeleteTeamCommand(Guid TeamId) : IRequest<Result>;
