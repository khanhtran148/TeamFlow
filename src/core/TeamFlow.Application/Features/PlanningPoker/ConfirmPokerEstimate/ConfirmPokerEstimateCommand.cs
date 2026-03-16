using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.PlanningPoker.ConfirmPokerEstimate;

public sealed record ConfirmPokerEstimateCommand(
    Guid SessionId,
    decimal FinalEstimate
) : IRequest<Result<PokerSessionDto>>;
