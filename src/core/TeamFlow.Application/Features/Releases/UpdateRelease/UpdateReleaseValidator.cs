using FluentValidation;

namespace TeamFlow.Application.Features.Releases.UpdateRelease;

public sealed class UpdateReleaseValidator : AbstractValidator<UpdateReleaseCommand>
{
    public UpdateReleaseValidator()
    {
        RuleFor(x => x.ReleaseId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
