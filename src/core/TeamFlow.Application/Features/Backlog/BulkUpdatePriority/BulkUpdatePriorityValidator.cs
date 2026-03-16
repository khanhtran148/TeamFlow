using FluentValidation;

namespace TeamFlow.Application.Features.Backlog.BulkUpdatePriority;

public sealed class BulkUpdatePriorityValidator : AbstractValidator<BulkUpdatePriorityCommand>
{
    public BulkUpdatePriorityValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("Items list cannot be empty");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.WorkItemId).NotEmpty();
            item.RuleFor(x => x.Priority).IsInEnum();
        });
    }
}
