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

        user1.Email.Should().NotBe("test@teamflow.dev");
        user2.Email.Should().NotBe("test@teamflow.dev");
    }

    [Fact]
    public void UserBuilder_New_GeneratesDifferentNames()
    {
        var user1 = UserBuilder.New().Build();
        var user2 = UserBuilder.New().Build();

        user1.Name.Should().NotBe("Test User");
        user2.Name.Should().NotBe("Test User");
    }

    [Fact]
    public void OrganizationBuilder_New_GeneratesDifferentNames()
    {
        var org1 = OrganizationBuilder.New().Build();
        var org2 = OrganizationBuilder.New().Build();

        org1.Name.Should().NotBe("Test Organization");
        org2.Name.Should().NotBe("Test Organization");
    }

    [Fact]
    public void ProjectBuilder_New_GeneratesDifferentNames()
    {
        var project1 = ProjectBuilder.New().Build();
        var project2 = ProjectBuilder.New().Build();

        project1.Name.Should().NotBe("Test Project");
        project2.Name.Should().NotBe("Test Project");
    }

    [Fact]
    public void WorkItemBuilder_New_GeneratesDifferentTitles()
    {
        var item1 = WorkItemBuilder.New().Build();
        var item2 = WorkItemBuilder.New().Build();

        item1.Title.Should().NotBe("Test Work Item");
        item2.Title.Should().NotBe("Test Work Item");
    }

    [Fact]
    public void SprintBuilder_New_GeneratesDifferentNames()
    {
        var sprint1 = SprintBuilder.New().Build();
        var sprint2 = SprintBuilder.New().Build();

        sprint1.Name.Should().NotBe("Test Sprint");
        sprint2.Name.Should().NotBe("Test Sprint");
    }

    [Fact]
    public void ReleaseBuilder_New_GeneratesDifferentNames()
    {
        var release1 = ReleaseBuilder.New().Build();
        var release2 = ReleaseBuilder.New().Build();

        release1.Name.Should().NotBe("v1.0.0");
        release2.Name.Should().NotBe("v1.0.0");
    }

    [Fact]
    public void TeamBuilder_New_GeneratesDifferentNames()
    {
        var team1 = TeamBuilder.New().Build();
        var team2 = TeamBuilder.New().Build();

        team1.Name.Should().NotBe("Test Team");
        team2.Name.Should().NotBe("Test Team");
    }

    [Fact]
    public void UserBuilder_WithEmail_OverridesGeneratedValue()
    {
        const string specificEmail = "override@example.com";
        var user = UserBuilder.New().WithEmail(specificEmail).Build();

        user.Email.Should().Be(specificEmail);
    }

    [Fact]
    public void UserBuilder_WithName_OverridesGeneratedValue()
    {
        const string specificName = "Specific Name";
        var user = UserBuilder.New().WithName(specificName).Build();

        user.Name.Should().Be(specificName);
    }

    [Fact]
    public void OrganizationBuilder_WithName_OverridesGeneratedValue()
    {
        const string specificName = "Specific Org";
        var org = OrganizationBuilder.New().WithName(specificName).Build();

        org.Name.Should().Be(specificName);
    }

    [Fact]
    public void ProjectBuilder_WithName_OverridesGeneratedValue()
    {
        const string specificName = "Specific Project";
        var project = ProjectBuilder.New().WithName(specificName).Build();

        project.Name.Should().Be(specificName);
    }

    [Fact]
    public void WorkItemBuilder_WithTitle_OverridesGeneratedValue()
    {
        const string specificTitle = "Specific Title";
        var item = WorkItemBuilder.New().WithTitle(specificTitle).Build();

        item.Title.Should().Be(specificTitle);
    }

    [Fact]
    public void SprintBuilder_WithName_OverridesGeneratedValue()
    {
        const string specificName = "Sprint 42";
        var sprint = SprintBuilder.New().WithName(specificName).Build();

        sprint.Name.Should().Be(specificName);
    }

    [Fact]
    public void ReleaseBuilder_WithName_OverridesGeneratedValue()
    {
        const string specificName = "v2.5.0";
        var release = ReleaseBuilder.New().WithName(specificName).Build();

        release.Name.Should().Be(specificName);
    }

    [Fact]
    public void TeamBuilder_WithName_OverridesGeneratedValue()
    {
        const string specificName = "Specific Team";
        var team = TeamBuilder.New().WithName(specificName).Build();

        team.Name.Should().Be(specificName);
    }
}
