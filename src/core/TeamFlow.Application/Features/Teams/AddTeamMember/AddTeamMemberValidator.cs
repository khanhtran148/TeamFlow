using FluentValidation;

namespace TeamFlow.Application.Features.Teams.AddTeamMember;

public sealed class AddTeamMemberValidator : AbstractValidator<AddTeamMemberCommand>
{
    public AddTeamMemberValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Role).IsInEnum();
    }
}
