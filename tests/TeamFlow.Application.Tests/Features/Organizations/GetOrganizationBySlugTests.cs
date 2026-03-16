using FluentAssertions;
using TeamFlow.Application.Features.Organizations.GetBySlug;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Organizations;

[Collection("Projects")]
public sealed class GetOrganizationBySlugTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_MemberOfOrg_ReturnsOrgDto()
    {
        var org = OrganizationBuilder.New().WithName("Test Org").WithSlug("test-org-get-" + Guid.NewGuid().ToString("N")[..6]).Build();
        DbContext.Organizations.Add(org);
        var member = OrganizationMemberBuilder.New()
            .WithOrganization(org.Id)
            .WithUser(SeedUserId)
            .Build();
        DbContext.OrganizationMembers.Add(member);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetOrganizationBySlugQuery(org.Slug));

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Org");
        result.Value.Slug.Should().Be(org.Slug);
    }

    [Fact]
    public async Task Handle_NonMember_ReturnsForbidden()
    {
        var org = OrganizationBuilder.New().WithSlug("org-no-member-" + Guid.NewGuid().ToString("N")[..6]).Build();
        DbContext.Organizations.Add(org);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetOrganizationBySlugQuery(org.Slug));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("access denied", o => o.IgnoringCase());
    }

    [Fact]
    public async Task Handle_OrgNotFound_ReturnsNotFound()
    {
        var result = await Sender.Send(new GetOrganizationBySlugQuery("nonexistent-slug-xyz"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().ContainEquivalentOf("not found", o => o.IgnoringCase());
    }
}
