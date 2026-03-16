using FluentValidation;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Organizations.CreateOrganization;

public sealed class CreateOrganizationValidator : AbstractValidator<CreateOrganizationCommand>
{
    public CreateOrganizationValidator(IOrganizationRepository organizationRepository)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        When(x => x.Slug is not null, () =>
        {
            RuleFor(x => x.Slug!)
                .MinimumLength(3)
                .MaximumLength(50)
                .Matches(@"^[a-z0-9][a-z0-9\-]*[a-z0-9]$")
                    .WithMessage("Slug must be lowercase alphanumeric with hyphens, not starting or ending with a hyphen.")
                .MustAsync(async (slug, ct) => !await organizationRepository.ExistsBySlugAsync(slug, ct))
                    .WithMessage("Slug already exists.")
                .WithName(nameof(CreateOrganizationCommand.Slug));
        });
    }
}
