using FluentAssertions;
using TeamFlow.Application.Features.Organizations.ListMyOrganizations;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Organizations;

[Collection("Projects")]
public sealed class ListMyOrganizationsTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_UserIsMemberOfOrgs_ReturnsMemberOrgDtos()
    {
        var org1 = OrganizationBuilder.New().WithName("Org A").WithSlug("org-a-" + Guid.NewGuid().ToString("N")[..6]).Build();
        var org2 = OrganizationBuilder.New().WithName("Org B").WithSlug("org-b-" + Guid.NewGuid().ToString("N")[..6]).Build();
        DbContext.Organizations.AddRange(org1, org2);

        var m1 = OrganizationMemberBuilder.New().WithOrganization(org1.Id).WithUser(SeedUserId).WithRole(OrgRole.Owner).Build();
        var m2 = OrganizationMemberBuilder.New().WithOrganization(org2.Id).WithUser(SeedUserId).WithRole(OrgRole.Member).Build();
        DbContext.OrganizationMembers.AddRange(m1, m2);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListMyOrganizationsQuery());

        result.IsSuccess.Should().BeTrue();
        var items = result.Value.ToList();
        items.Should().Contain(o => o.Name == "Org A" && o.Role == OrgRole.Owner);
        items.Should().Contain(o => o.Name == "Org B" && o.Role == OrgRole.Member);
    }

    [Fact]
    public async Task Handle_UserHasNoMemberships_ReturnsEmptyList()
    {
        // SeedUserId has no org memberships in this transaction
        var result = await Sender.Send(new ListMyOrganizationsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
