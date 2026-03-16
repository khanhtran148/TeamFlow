using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Comments.DeleteComment;

public sealed record DeleteCommentCommand(Guid CommentId) : IRequest<Result>;
