namespace TeamFlow.Domain.Entities;

public sealed class Comment : BaseEntity
{
    public Guid WorkItemId { get; set; }
    public Guid AuthorId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public WorkItem? WorkItem { get; set; }
    public User? Author { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = [];
}
