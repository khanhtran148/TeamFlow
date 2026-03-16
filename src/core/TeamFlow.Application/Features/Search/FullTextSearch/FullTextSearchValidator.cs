using FluentValidation;

namespace TeamFlow.Application.Features.Search.FullTextSearch;

public sealed class FullTextSearchValidator : AbstractValidator<FullTextSearchQuery>
{
    public FullTextSearchValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
