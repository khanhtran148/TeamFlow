using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SystemRole SystemRole { get; set; } = SystemRole.User;

    // Navigation
    public ICollection<TeamMember> TeamMemberships { get; set; } = [];
    public ICollection<ProjectMembership> ProjectMemberships { get; set; } = [];
    public ICollection<WorkItem> AssignedWorkItems { get; set; } = [];
}
