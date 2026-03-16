using FluentValidation;

namespace TeamFlow.Application.Features.Search.SaveFilter;

public sealed class SaveFilterValidator : AbstractValidator<SaveFilterCommand>
{
    public SaveFilterValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FilterJson).NotNull();
    }
}
