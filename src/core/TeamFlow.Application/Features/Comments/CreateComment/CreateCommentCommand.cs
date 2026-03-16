using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Comments.CreateComment;

public sealed record CreateCommentCommand(
    Guid WorkItemId,
    string Content,
    Guid? ParentCommentId
) : IRequest<Result<CommentDto>>;
