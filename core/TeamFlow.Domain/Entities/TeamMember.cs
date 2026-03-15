using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public class TeamMember
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid TeamId { get; set; }
    public Guid UserId { get; set; }
    public ProjectRole Role { get; set; }
    public DateTime JoinedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public Team? Team { get; set; }
    public User? User { get; set; }
}
