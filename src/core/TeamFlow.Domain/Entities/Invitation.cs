using TeamFlow.Domain.Enums;

namespace TeamFlow.Domain.Entities;

public sealed class Invitation
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid InvitedByUserId { get; set; }
    public string? Email { get; set; }
    public OrgRole Role { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public InviteStatus Status { get; set; } = InviteStatus.Pending;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? AcceptedAt { get; set; }
    public Guid? AcceptedByUserId { get; set; }

    // Navigation
    public Organization? Organization { get; set; }
    public User? InvitedBy { get; set; }
    public User? AcceptedBy { get; set; }
}
