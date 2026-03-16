using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.PlanningPoker.CreatePokerSession;

public sealed record CreatePokerSessionCommand(Guid WorkItemId) : IRequest<Result<PokerSessionDto>>;
