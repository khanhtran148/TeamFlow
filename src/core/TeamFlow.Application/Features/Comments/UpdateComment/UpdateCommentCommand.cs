using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Comments.UpdateComment;

public sealed record UpdateCommentCommand(
    Guid CommentId,
    string Content
) : IRequest<Result<CommentDto>>;
