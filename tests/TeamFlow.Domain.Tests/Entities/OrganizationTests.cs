using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Domain.Tests.Entities;

public sealed class OrganizationTests
{
    [Fact]
    public void Organization_Build_ShouldHaveSlugProperty()
    {
        var org = OrganizationBuilder.New()
            .WithName("Acme Corp")
            .Build();

        // Slug should not be null
        org.Slug.Should().NotBeNull();
    }

    [Fact]
    public void Organization_Build_WithExplicitSlug_ShouldUseProvidedSlug()
    {
        var org = OrganizationBuilder.New()
            .WithName("Acme Corp")
            .WithSlug("acme-corp")
            .Build();

        org.Slug.Should().Be("acme-corp");
    }

    [Fact]
    public void Organization_ShouldHaveUpdatedAtProperty()
    {
        var org = OrganizationBuilder.New().Build();

        org.UpdatedAt.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void Organization_ShouldHaveMembersNavigation()
    {
        var org = OrganizationBuilder.New().Build();

        org.Members.Should().NotBeNull();
        org.Members.Should().BeEmpty();
    }

    [Theory]
    [InlineData("My Company", "my-company")]
    [InlineData("Acme Corp 123", "acme-corp-123")]
    [InlineData("  Leading Spaces  ", "leading-spaces")]
    [InlineData("Special!@#$Chars", "specialchars")]
    [InlineData("Multiple   Spaces", "multiple-spaces")]
    public void Organization_SlugFromName_ShouldGenerateCorrectSlug(string name, string expectedSlug)
    {
        var slug = Organization.GenerateSlug(name);
        slug.Should().Be(expectedSlug);
    }
}
