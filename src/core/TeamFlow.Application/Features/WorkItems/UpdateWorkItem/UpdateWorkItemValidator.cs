using FluentValidation;

namespace TeamFlow.Application.Features.WorkItems.UpdateWorkItem;

public sealed class UpdateWorkItemValidator : AbstractValidator<UpdateWorkItemCommand>
{
    public UpdateWorkItemValidator()
    {
        RuleFor(x => x.WorkItemId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(10000).When(x => x.Description is not null);
        RuleFor(x => x.EstimationValue).GreaterThanOrEqualTo(0).When(x => x.EstimationValue.HasValue);
    }
}
