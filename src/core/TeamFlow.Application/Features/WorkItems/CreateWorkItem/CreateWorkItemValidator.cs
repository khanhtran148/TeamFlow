using FluentValidation;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.WorkItems.CreateWorkItem;

public sealed class CreateWorkItemValidator : AbstractValidator<CreateWorkItemCommand>
{
    public CreateWorkItemValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(10000).When(x => x.Description is not null);
    }
}
