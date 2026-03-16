using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.PlanningPoker.GetPokerSession;

public sealed record GetPokerSessionQuery(Guid? SessionId, Guid? WorkItemId) : IRequest<Result<PokerSessionDto>>;
