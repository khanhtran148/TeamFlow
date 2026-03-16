using FluentValidation;

namespace TeamFlow.Application.Features.Retros.SubmitRetroCard;

public sealed class SubmitRetroCardValidator : AbstractValidator<SubmitRetroCardCommand>
{
    public SubmitRetroCardValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}
