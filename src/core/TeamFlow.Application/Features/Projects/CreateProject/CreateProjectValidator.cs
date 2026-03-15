using FluentValidation;

namespace TeamFlow.Application.Features.Projects.CreateProject;

public sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.OrgId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}
