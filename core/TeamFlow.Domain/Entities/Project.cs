namespace TeamFlow.Domain.Entities;

public class Project : BaseEntity
{
    public Guid OrgId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "Active"; // Active, Archived

    // Navigation
    public Organization? Organization { get; set; }
    public ICollection<ProjectMembership> Memberships { get; set; } = [];
    public ICollection<WorkItem> WorkItems { get; set; } = [];
    public ICollection<Sprint> Sprints { get; set; } = [];
    public ICollection<Release> Releases { get; set; } = [];
    public ICollection<RetroSession> RetroSessions { get; set; } = [];
}
