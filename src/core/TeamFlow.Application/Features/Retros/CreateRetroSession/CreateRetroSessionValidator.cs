using FluentValidation;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Retros.CreateRetroSession;

public sealed class CreateRetroSessionValidator : AbstractValidator<CreateRetroSessionCommand>
{
    public CreateRetroSessionValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.AnonymityMode).Must(m => RetroAnonymityModes.All.Contains(m))
            .WithMessage("AnonymityMode must be 'Public' or 'Anonymous'");
    }
}
