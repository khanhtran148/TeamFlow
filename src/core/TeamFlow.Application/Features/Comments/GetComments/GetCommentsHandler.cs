using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Comments.GetComments;

public sealed class GetCommentsHandler(
    ICommentRepository commentRepository,
    IWorkItemRepository workItemRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<GetCommentsQuery, Result<GetCommentsResponse>>
{
    public async Task<Result<GetCommentsResponse>> Handle(GetCommentsQuery request, CancellationToken ct)
    {
        var workItem = await workItemRepository.GetByIdAsync(request.WorkItemId, ct);
        if (workItem is null)
            return DomainError.NotFound<GetCommentsResponse>("Work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, workItem.ProjectId, Permission.Comment_View, ct))
            return DomainError.Forbidden<GetCommentsResponse>();

        var (items, totalCount) = await commentRepository.GetByWorkItemPagedAsync(
            request.WorkItemId, request.Page, request.PageSize, ct);

        var dtos = items.Select(MapToDto).ToList();

        return Result.Success(new GetCommentsResponse(dtos, totalCount, request.Page, request.PageSize));
    }

    private static CommentDto MapToDto(Comment comment) =>
        new(
            comment.Id,
            comment.WorkItemId,
            comment.AuthorId,
            comment.Author?.Name ?? "Unknown",
            comment.ParentCommentId,
            comment.Content,
            comment.EditedAt,
            comment.CreatedAt,
            comment.Replies.Select(MapToDto).ToList()
        );
}
