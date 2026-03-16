using FluentValidation;

namespace TeamFlow.Application.Features.Search.UpdateSavedFilter;

public sealed class UpdateSavedFilterValidator : AbstractValidator<UpdateSavedFilterCommand>
{
    public UpdateSavedFilterValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.FilterId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(100).When(x => x.Name is not null);
    }
}
