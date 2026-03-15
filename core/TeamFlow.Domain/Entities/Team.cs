namespace TeamFlow.Domain.Entities;

public class Team : BaseEntity
{
    public Guid OrgId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation
    public Organization? Organization { get; set; }
    public ICollection<TeamMember> Members { get; set; } = [];
}
