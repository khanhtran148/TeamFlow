using FluentValidation;

namespace TeamFlow.Application.Features.Comments.CreateComment;

public sealed class CreateCommentValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentValidator()
    {
        RuleFor(x => x.WorkItemId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(10000);
    }
}
