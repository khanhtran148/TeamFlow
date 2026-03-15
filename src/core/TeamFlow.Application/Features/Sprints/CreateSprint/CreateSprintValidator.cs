using FluentValidation;

namespace TeamFlow.Application.Features.Sprints.CreateSprint;

public sealed class CreateSprintValidator : AbstractValidator<CreateSprintCommand>
{
    public CreateSprintValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EndDate)
            .Must((cmd, endDate) => endDate is null || cmd.StartDate is null || endDate > cmd.StartDate)
            .WithMessage("End date must be after start date");
    }
}
