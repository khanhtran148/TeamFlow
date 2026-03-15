using FluentValidation;

namespace TeamFlow.Application.Features.Sprints.UpdateCapacity;

public sealed class UpdateCapacityValidator : AbstractValidator<UpdateCapacityCommand>
{
    public UpdateCapacityValidator()
    {
        RuleFor(x => x.SprintId).NotEmpty();
        RuleFor(x => x.Capacity).NotEmpty();
        RuleForEach(x => x.Capacity).ChildRules(entry =>
        {
            entry.RuleFor(e => e.MemberId).NotEmpty();
            entry.RuleFor(e => e.Points).GreaterThan(0)
                .WithMessage("Capacity points must be greater than zero");
        });
    }
}
