using TeamFlow.Domain.Entities;

namespace TeamFlow.Tests.Common.Builders;

public sealed class OrganizationBuilder
{
    private string _name = "Test Organization";

    public static OrganizationBuilder New() => new();

    public OrganizationBuilder WithName(string name) { _name = name; return this; }

    public Organization Build() => new()
    {
        Name = _name
    };
}
