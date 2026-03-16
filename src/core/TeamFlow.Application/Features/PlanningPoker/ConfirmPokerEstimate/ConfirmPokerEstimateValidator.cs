using FluentValidation;

namespace TeamFlow.Application.Features.PlanningPoker.ConfirmPokerEstimate;

public sealed class ConfirmPokerEstimateValidator : AbstractValidator<ConfirmPokerEstimateCommand>
{
    private static readonly decimal[] AllowedValues = [1, 2, 3, 5, 8, 13, 21];

    public ConfirmPokerEstimateValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.FinalEstimate)
            .Must(v => AllowedValues.Contains(v))
            .WithMessage("Final estimate must be a valid Fibonacci value (1, 2, 3, 5, 8, 13, 21)");
    }
}
