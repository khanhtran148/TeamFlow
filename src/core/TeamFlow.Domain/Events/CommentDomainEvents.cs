using MediatR;

namespace TeamFlow.Domain.Events;

public sealed record CommentCreatedDomainEvent(
    Guid CommentId,
    Guid WorkItemId,
    Guid ProjectId,
    Guid AuthorId,
    Guid? ParentCommentId
) : INotification;

public sealed record CommentMentionDomainEvent(
    Guid CommentId,
    Guid WorkItemId,
    Guid ProjectId,
    Guid MentionedUserId,
    Guid AuthorId
) : INotification;
