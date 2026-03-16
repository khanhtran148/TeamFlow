using FluentValidation;

namespace TeamFlow.Application.Features.Comments.UpdateComment;

public sealed class UpdateCommentValidator : AbstractValidator<UpdateCommentCommand>
{
    public UpdateCommentValidator()
    {
        RuleFor(x => x.CommentId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(10000);
    }
}
