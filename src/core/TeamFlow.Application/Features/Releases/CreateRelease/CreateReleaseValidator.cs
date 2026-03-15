using FluentValidation;

namespace TeamFlow.Application.Features.Releases.CreateRelease;

public sealed class CreateReleaseValidator : AbstractValidator<CreateReleaseCommand>
{
    public CreateReleaseValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}
