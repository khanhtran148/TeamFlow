using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Tests.Common.Builders;

public sealed class TeamBuilder
{
    private Guid _orgId = Guid.NewGuid();
    private string _name = "Test Team";
    private string? _description;
    private readonly List<TeamMember> _members = [];

    public static TeamBuilder New() => new();

    public TeamBuilder WithOrg(Guid orgId) { _orgId = orgId; return this; }
    public TeamBuilder WithName(string name) { _name = name; return this; }
    public TeamBuilder WithDescription(string? description) { _description = description; return this; }

    public TeamBuilder WithMember(Guid userId, ProjectRole role = ProjectRole.Developer)
    {
        _members.Add(new TeamMember { TeamId = Guid.Empty, UserId = userId, Role = role });
        return this;
    }

    public Team Build()
    {
        var team = new Team
        {
            OrgId = _orgId,
            Name = _name,
            Description = _description
        };
        foreach (var m in _members)
            team.Members.Add(m);
        return team;
    }
}
