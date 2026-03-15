using FluentValidation;

namespace TeamFlow.Application.Features.ProjectMemberships.AddProjectMember;

public sealed class AddProjectMemberValidator : AbstractValidator<AddProjectMemberCommand>
{
    private static readonly string[] ValidMemberTypes = ["User", "Team"];

    public AddProjectMemberValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.MemberId).NotEmpty();
        RuleFor(x => x.MemberType)
            .NotEmpty()
            .Must(t => ValidMemberTypes.Contains(t))
            .WithMessage("MemberType must be 'User' or 'Team'");
        RuleFor(x => x.Role).IsInEnum();
    }
}
