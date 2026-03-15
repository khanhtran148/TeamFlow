using Bogus;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Fakers;

namespace TeamFlow.Tests.Common.Builders;

public sealed class ProjectBuilder
{
    private static readonly Faker F = FakerProvider.Instance;

    private Guid _orgId = Guid.NewGuid();
    private string _name = F.Commerce.ProductName();
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
