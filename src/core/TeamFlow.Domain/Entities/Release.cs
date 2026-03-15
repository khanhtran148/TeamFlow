using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public class Release
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? ReleaseDate { get; set; }
    public ReleaseStatus Status { get; set; } = ReleaseStatus.Unreleased;
    public DateTime? ReleasedAt { get; set; }
    public Guid? ReleasedById { get; set; }
    public bool NotesLocked { get; set; } = false;
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public Project? Project { get; set; }
    public User? ReleasedBy { get; set; }
    public ICollection<WorkItem> WorkItems { get; set; } = [];
}
