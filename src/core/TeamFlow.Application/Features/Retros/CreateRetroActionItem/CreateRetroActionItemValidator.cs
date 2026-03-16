using FluentValidation;

namespace TeamFlow.Application.Features.Retros.CreateRetroActionItem;

public sealed class CreateRetroActionItemValidator : AbstractValidator<CreateRetroActionItemCommand>
{
    public CreateRetroActionItemValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}
