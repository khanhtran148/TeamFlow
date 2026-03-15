namespace TeamFlow.Domain.Entities;

public class Organization
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Team> Teams { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
}
