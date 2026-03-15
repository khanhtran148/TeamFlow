using Bogus;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Fakers;

namespace TeamFlow.Tests.Common.Builders;

public sealed class TeamBuilder
{
    private static readonly Faker F = FakerProvider.Instance;

    private Guid _orgId = Guid.NewGuid();
    private string _name = F.Commerce.Department();
    private string? _description;
    private readonly List<(Guid UserId, ProjectRole Role)> _members = [];

    public static TeamBuilder New() => new();

    public TeamBuilder WithOrganization(Guid orgId) { _orgId = orgId; return this; }
    public TeamBuilder WithName(string name) { _name = name; return this; }
    public TeamBuilder WithDescription(string? description) { _description = description; return this; }

    public TeamBuilder WithMember(Guid userId, ProjectRole role = ProjectRole.Developer)
    {
        _members.Add((userId, role));
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
        foreach (var (userId, role) in _members)
            team.Members.Add(new TeamMember { TeamId = team.Id, UserId = userId, Role = role });
        return team;
    }
}
