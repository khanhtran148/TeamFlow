using System.Text.Json;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public class ProjectMembership
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberType { get; set; } = "User"; // User, Team
    public ProjectRole Role { get; set; }
    public JsonDocument? CustomPermissions { get; set; } // Individual-level overrides
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public Project? Project { get; set; }
}
