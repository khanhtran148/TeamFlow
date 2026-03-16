using FluentValidation;

namespace TeamFlow.Application.Features.Admin.ChangeOrgStatus;

public sealed class AdminChangeOrgStatusValidator : AbstractValidator<AdminChangeOrgStatusCommand>
{
    public AdminChangeOrgStatusValidator()
    {
        RuleFor(x => x.OrgId).NotEmpty().WithMessage("OrgId is required.");
    }
}
