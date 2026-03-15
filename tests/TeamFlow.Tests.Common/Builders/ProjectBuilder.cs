using TeamFlow.Domain.Entities;

namespace TeamFlow.Tests.Common.Builders;

public sealed class ProjectBuilder
{
    private Guid _orgId = Guid.NewGuid();
    private string _name = "Test Project";
    private string? _description;
    private string _status = "Active";

    public static ProjectBuilder New() => new();

    public ProjectBuilder WithOrganization(Guid orgId) { _orgId = orgId; return this; }
    public ProjectBuilder WithName(string name) { _name = name; return this; }
    public ProjectBuilder WithDescription(string description) { _description = description; return this; }
    public ProjectBuilder WithStatus(string status) { _status = status; return this; }
    public ProjectBuilder Archived() { _status = "Archived"; return this; }

    public Project Build() => new()
    {
        OrgId = _orgId,
        Name = _name,
        Description = _description,
        Status = _status
    };
}
