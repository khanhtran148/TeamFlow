using Bogus;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Fakers;

namespace TeamFlow.Tests.Common.Builders;

public sealed class CommentBuilder
{
    private static readonly Faker F = FakerProvider.Instance;

    private Guid _workItemId = Guid.NewGuid();
    private Guid _authorId = Guid.NewGuid();
    private Guid? _parentCommentId;
    private string _content = F.Lorem.Paragraph();
    private DateTime? _editedAt;
    private DateTime? _deletedAt;

    public static CommentBuilder New() => new();

    public CommentBuilder WithWorkItem(Guid workItemId) { _workItemId = workItemId; return this; }
    public CommentBuilder WithAuthor(Guid authorId) { _authorId = authorId; return this; }
    public CommentBuilder WithParent(Guid parentCommentId) { _parentCommentId = parentCommentId; return this; }
    public CommentBuilder WithContent(string content) { _content = content; return this; }
    public CommentBuilder Edited() { _editedAt = DateTime.UtcNow; return this; }
    public CommentBuilder Deleted() { _deletedAt = DateTime.UtcNow; return this; }

    public Comment Build() => new()
    {
        WorkItemId = _workItemId,
        AuthorId = _authorId,
        ParentCommentId = _parentCommentId,
        Content = _content,
        EditedAt = _editedAt,
        DeletedAt = _deletedAt
    };
}
