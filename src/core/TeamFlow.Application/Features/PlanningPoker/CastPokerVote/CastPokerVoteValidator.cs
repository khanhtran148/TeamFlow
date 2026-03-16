using FluentValidation;

namespace TeamFlow.Application.Features.PlanningPoker.CastPokerVote;

public sealed class CastPokerVoteValidator : AbstractValidator<CastPokerVoteCommand>
{
    private static readonly decimal[] FibonacciValues = [1, 2, 3, 5, 8, 13, 21];

    public CastPokerVoteValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Value).Must(v => FibonacciValues.Contains(v))
            .WithMessage("Value must be a Fibonacci number: 1, 2, 3, 5, 8, 13, or 21");
    }
}
