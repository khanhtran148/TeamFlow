using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.PlanningPoker.RevealPokerVotes;

public sealed record RevealPokerVotesCommand(Guid SessionId) : IRequest<Result<PokerSessionDto>>;
