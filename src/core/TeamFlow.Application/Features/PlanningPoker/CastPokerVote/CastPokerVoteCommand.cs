using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.PlanningPoker.CastPokerVote;

public sealed record CastPokerVoteCommand(Guid SessionId, decimal Value) : IRequest<Result>;
