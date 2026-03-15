using FluentValidation;

namespace TeamFlow.Application.Features.Teams.ListTeams;

public sealed class ListTeamsValidator : AbstractValidator<ListTeamsQuery>
{
    public ListTeamsValidator()
    {
        RuleFor(x => x.OrgId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
