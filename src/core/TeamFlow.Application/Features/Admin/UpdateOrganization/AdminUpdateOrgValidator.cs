using FluentValidation;

namespace TeamFlow.Application.Features.Admin.UpdateOrganization;

public sealed class AdminUpdateOrgValidator : AbstractValidator<AdminUpdateOrgCommand>
{
    public AdminUpdateOrgValidator()
    {
        RuleFor(x => x.OrgId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(50)
            .Matches(@"^[a-z0-9-]+$")
            .WithMessage("Slug must contain only lowercase letters, digits, and hyphens.");
    }
}
