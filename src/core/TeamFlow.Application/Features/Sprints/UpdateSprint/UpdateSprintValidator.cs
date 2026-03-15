using FluentValidation;

namespace TeamFlow.Application.Features.Sprints.UpdateSprint;

public sealed class UpdateSprintValidator : AbstractValidator<UpdateSprintCommand>
{
    public UpdateSprintValidator()
    {
        RuleFor(x => x.SprintId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EndDate)
            .Must((cmd, endDate) => endDate is null || cmd.StartDate is null || endDate > cmd.StartDate)
            .WithMessage("End date must be after start date");
    }
}
