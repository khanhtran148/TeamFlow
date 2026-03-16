using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Comments.CreateComment;

public sealed partial class CreateCommentHandler(
    ICommentRepository commentRepository,
    IWorkItemRepository workItemRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<CreateCommentCommand, Result<CommentDto>>
{
    [GeneratedRegex(@"@([a-zA-Z0-9._\-]+)")]
    private static partial Regex MentionRegex();

    public async Task<Result<CommentDto>> Handle(CreateCommentCommand request, CancellationToken ct)
    {
        var workItem = await workItemRepository.GetByIdAsync(request.WorkItemId, ct);
        if (workItem is null)
            return DomainError.NotFound<CommentDto>("Work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, workItem.ProjectId, Permission.Comment_Create, ct))
            return DomainError.Forbidden<CommentDto>();

        if (request.ParentCommentId.HasValue)
        {
            var parent = await commentRepository.GetByIdAsync(request.ParentCommentId.Value, ct);
            if (parent is null || parent.DeletedAt is not null)
                return DomainError.NotFound<CommentDto>("Parent comment not found");

            if (parent.ParentCommentId is not null)
                return DomainError.Validation<CommentDto>("Nested replies are not allowed");
        }

        var comment = new Comment
        {
            WorkItemId = request.WorkItemId,
            AuthorId = currentUser.Id,
            ParentCommentId = request.ParentCommentId,
            Content = request.Content
        };

        await commentRepository.AddAsync(comment, ct);

        await publisher.Publish(new CommentCreatedDomainEvent(
            comment.Id, comment.WorkItemId, workItem.ProjectId, currentUser.Id, comment.ParentCommentId), ct);

        // Parse @mentions and publish events
        var mentions = MentionRegex().Matches(request.Content);
        if (mentions.Count > 0)
        {
            var mentionNames = mentions.Select(m => m.Groups[1].Value).Distinct().ToList();
            var mentionedUsers = await userRepository.GetByDisplayNamesAsync(mentionNames, ct);
            foreach (var mentionedUser in mentionedUsers)
            {
                if (mentionedUser.Id != currentUser.Id)
                {
                    await publisher.Publish(new CommentMentionDomainEvent(
                        comment.Id, comment.WorkItemId, workItem.ProjectId, mentionedUser.Id, currentUser.Id), ct);
                }
            }
        }

        var author = await userRepository.GetByIdAsync(currentUser.Id, ct);

        return Result.Success(MapToDto(comment, author?.Name ?? "Unknown"));
    }

    private static CommentDto MapToDto(Comment comment, string authorName) =>
        new(
            comment.Id,
            comment.WorkItemId,
            comment.AuthorId,
            authorName,
            comment.ParentCommentId,
            comment.Content,
            comment.EditedAt,
            comment.CreatedAt,
            []
        );
}
