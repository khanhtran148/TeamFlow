using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public class WorkItemLink
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid SourceId { get; set; }
    public Guid TargetId { get; set; }
    public LinkType LinkType { get; set; }
    public LinkScope Scope { get; set; } = LinkScope.SameProject;
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public WorkItem? Source { get; set; }
    public WorkItem? Target { get; set; }
    public User? CreatedBy { get; set; }
}
