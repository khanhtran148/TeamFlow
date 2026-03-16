using FluentValidation;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Organizations.UpdateOrganization;

public sealed class UpdateOrganizationValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationValidator(IOrganizationRepository organizationRepository)
    {
        RuleFor(x => x.OrgId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches(@"^[a-z0-9][a-z0-9\-]*[a-z0-9]$")
                .WithMessage("Slug must be lowercase alphanumeric with hyphens, not starting or ending with a hyphen.");
    }
}
