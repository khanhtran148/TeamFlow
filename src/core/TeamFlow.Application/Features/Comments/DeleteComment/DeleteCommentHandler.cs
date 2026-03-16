using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Comments.DeleteComment;

public sealed class DeleteCommentHandler(
    ICommentRepository commentRepository,
    IWorkItemRepository workItemRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<DeleteCommentCommand, Result>
{
    public async Task<Result> Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        var comment = await commentRepository.GetByIdAsync(request.CommentId, ct);
        if (comment is null)
            return DomainError.NotFound("Comment not found");

        if (comment.DeletedAt is not null)
            return DomainError.NotFound("Comment not found");

        var workItem = await workItemRepository.GetByIdAsync(comment.WorkItemId, ct);
        if (workItem is null)
            return DomainError.NotFound("Work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, workItem.ProjectId, Permission.Comment_DeleteOwn, ct))
            return DomainError.Forbidden();

        if (comment.AuthorId != currentUser.Id)
            return DomainError.Forbidden();

        comment.DeletedAt = DateTime.UtcNow;
        await commentRepository.UpdateAsync(comment, ct);

        return Result.Success();
    }
}
