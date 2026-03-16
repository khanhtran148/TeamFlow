namespace TeamFlow.Application.Features.Comments;

public sealed record CommentDto(
    Guid Id,
    Guid WorkItemId,
    Guid AuthorId,
    string AuthorName,
    Guid? ParentCommentId,
    string Content,
    DateTime? EditedAt,
    DateTime CreatedAt,
    IReadOnlyList<CommentDto> Replies
);
