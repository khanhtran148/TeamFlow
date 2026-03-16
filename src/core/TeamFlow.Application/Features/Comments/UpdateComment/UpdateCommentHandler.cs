using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Comments.UpdateComment;

public sealed class UpdateCommentHandler(
    ICommentRepository commentRepository,
    IWorkItemRepository workItemRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<UpdateCommentCommand, Result<CommentDto>>
{
    public async Task<Result<CommentDto>> Handle(UpdateCommentCommand request, CancellationToken ct)
    {
        var comment = await commentRepository.GetByIdAsync(request.CommentId, ct);
        if (comment is null)
            return DomainError.NotFound<CommentDto>("Comment not found");

        if (comment.DeletedAt is not null)
            return DomainError.NotFound<CommentDto>("Comment not found");

        var workItem = await workItemRepository.GetByIdAsync(comment.WorkItemId, ct);
        if (workItem is null)
            return DomainError.NotFound<CommentDto>("Work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, workItem.ProjectId, Permission.Comment_EditOwn, ct))
            return DomainError.Forbidden<CommentDto>();

        if (comment.AuthorId != currentUser.Id)
            return DomainError.Forbidden<CommentDto>();

        comment.Content = request.Content;
        comment.EditedAt = DateTime.UtcNow;

        await commentRepository.UpdateAsync(comment, ct);

        return Result.Success(new CommentDto(
            comment.Id,
            comment.WorkItemId,
            comment.AuthorId,
            comment.Author?.Name ?? "Unknown",
            comment.ParentCommentId,
            comment.Content,
            comment.EditedAt,
            comment.CreatedAt,
            []
        ));
    }
}
