using FluentValidation;

namespace TeamFlow.Application.Features.OrgMembers.ChangeRole;

public sealed class ChangeOrgMemberRoleValidator : AbstractValidator<ChangeOrgMemberRoleCommand>
{
    public ChangeOrgMemberRoleValidator()
    {
        RuleFor(x => x.OrgId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewRole).IsInEnum();
    }
}
