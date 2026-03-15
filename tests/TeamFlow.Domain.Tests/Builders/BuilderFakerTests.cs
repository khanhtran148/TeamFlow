using FluentAssertions;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Domain.Tests.Builders;

public sealed class BuilderFakerTests
{
    [Fact]
    public void UserBuilder_New_GeneratesDifferentEmails()
    {
        var user1 = UserBuilder.New().Build();
        var user2 = UserBuilder.New().Build();

        user1.Email.Should().NotBe(user2.Email);
    }

    [Fact]
    public void UserBuilder_New_GeneratesDifferentNames()
    {
        var user1 = UserBuilder.New().Build();
        var user2 = UserBuilder.New().Build();

        user1.Name.Should().NotBe(user2.Name);
    }

    [Fact]
    public void UserBuilder_New_UsesExampleDomainForEmails()
    {
        var user = UserBuilder.New().Build();

        user.Email.Should().EndWith("@example.com");
    }

    [Theory]
    [InlineData("Organization", nameof(OrganizationBuilder))]
    [InlineData("Project", nameof(ProjectBuilder))]
    [InlineData("WorkItem", nameof(WorkItemBuilder))]
    [InlineData("Sprint", nameof(SprintBuilder))]
    [InlineData("Release", nameof(ReleaseBuilder))]
    [InlineData("Team", nameof(TeamBuilder))]
    public void Builder_New_GeneratesDifferentStringValues(string _, string builderType)
    {
        var (value1, value2) = builderType switch
        {
            nameof(OrganizationBuilder) => (OrganizationBuilder.New().Build().Name, OrganizationBuilder.New().Build().Name),
            nameof(ProjectBuilder) => (ProjectBuilder.New().Build().Name, ProjectBuilder.New().Build().Name),
            nameof(WorkItemBuilder) => (WorkItemBuilder.New().Build().Title, WorkItemBuilder.New().Build().Title),
            nameof(SprintBuilder) => (SprintBuilder.New().Build().Name, SprintBuilder.New().Build().Name),
            nameof(ReleaseBuilder) => (ReleaseBuilder.New().Build().Name, ReleaseBuilder.New().Build().Name),
            nameof(TeamBuilder) => (TeamBuilder.New().Build().Name, TeamBuilder.New().Build().Name),
            _ => throw new ArgumentException($"Unknown builder type: {builderType}")
        };

        value1.Should().NotBe(value2);
    }

    [Theory]
    [InlineData("override@example.com", "Override User")]
    public void UserBuilder_WithOverrides_ReturnsExactValues(string email, string name)
    {
        var user = UserBuilder.New()
            .WithEmail(email)
            .WithName(name)
            .Build();

        user.Email.Should().Be(email);
        user.Name.Should().Be(name);
    }

    [Theory]
    [InlineData("Specific Org", nameof(OrganizationBuilder))]
    [InlineData("Specific Project", nameof(ProjectBuilder))]
    [InlineData("Specific Title", nameof(WorkItemBuilder))]
    [InlineData("Sprint 42", nameof(SprintBuilder))]
    [InlineData("v2.5.0", nameof(ReleaseBuilder))]
    [InlineData("Specific Team", nameof(TeamBuilder))]
    public void Builder_WithNameOrTitle_OverridesGeneratedValue(string overrideValue, string builderType)
    {
        var actualValue = builderType switch
        {
            nameof(OrganizationBuilder) => OrganizationBuilder.New().WithName(overrideValue).Build().Name,
            nameof(ProjectBuilder) => ProjectBuilder.New().WithName(overrideValue).Build().Name,
            nameof(WorkItemBuilder) => WorkItemBuilder.New().WithTitle(overrideValue).Build().Title,
            nameof(SprintBuilder) => SprintBuilder.New().WithName(overrideValue).Build().Name,
            nameof(ReleaseBuilder) => ReleaseBuilder.New().WithName(overrideValue).Build().Name,
            nameof(TeamBuilder) => TeamBuilder.New().WithName(overrideValue).Build().Name,
            _ => throw new ArgumentException($"Unknown builder type: {builderType}")
        };

        actualValue.Should().Be(overrideValue);
    }
}
