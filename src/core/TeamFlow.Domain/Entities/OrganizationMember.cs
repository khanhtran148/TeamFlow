using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public sealed class OrganizationMember
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public OrgRole Role { get; set; }
    public DateTime JoinedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation
    public Organization? Organization { get; set; }
    public User? User { get; set; }
}
