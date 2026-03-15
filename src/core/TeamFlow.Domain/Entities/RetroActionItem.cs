namespace TeamFlow.Domain.Entities;

public class RetroActionItem
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public Guid? CardId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AssigneeId { get; set; }
    public DateOnly? DueDate { get; set; }
    public Guid? LinkedTaskId { get; set; } // Bidirectional link to work_items
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public RetroSession? Session { get; set; }
    public RetroCard? Card { get; set; }
    public User? Assignee { get; set; }
    public WorkItem? LinkedTask { get; set; }
}
