using Bogus;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Fakers;

namespace TeamFlow.Tests.Common.Builders;

public sealed class OrganizationBuilder
{
    private static readonly Faker F = FakerProvider.Instance;

    private string _name = F.Company.CompanyName();

    public static OrganizationBuilder New() => new();

    public OrganizationBuilder WithName(string name) { _name = name; return this; }

    public Organization Build() => new()
    {
        Name = _name
    };
}
