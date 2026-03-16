using FluentAssertions;
using TeamFlow.Application.Features.Organizations.ListOrganizations;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Organizations;

[Collection("Projects")]
public sealed class ListOrganizationsTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_UserHasOrganizations_ReturnsMappedDtos()
    {
        var org1 = OrganizationBuilder.New().WithName("Org A").WithSlug("list-org-a-" + Guid.NewGuid().ToString("N")[..6]).Build();
        var org2 = OrganizationBuilder.New().WithName("Org B").WithSlug("list-org-b-" + Guid.NewGuid().ToString("N")[..6]).Build();
        DbContext.Organizations.AddRange(org1, org2);

        var m1 = OrganizationMemberBuilder.New().WithOrganization(org1.Id).WithUser(SeedUserId).Build();
        var m2 = OrganizationMemberBuilder.New().WithOrganization(org2.Id).WithUser(SeedUserId).Build();
        DbContext.OrganizationMembers.AddRange(m1, m2);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListOrganizationsQuery());

        result.IsSuccess.Should().BeTrue();
        var items = result.Value.ToList();
        items.Should().Contain(o => o.Name == "Org A");
        items.Should().Contain(o => o.Name == "Org B");
    }

    [Fact]
    public async Task Handle_UserHasNoOrganizations_ReturnsEmptyList()
    {
        // SeedUserId has no memberships in this transaction
        var result = await Sender.Send(new ListOrganizationsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_QueriesForCurrentUser()
    {
        // Ensure the current user's orgs are returned (not another user's orgs)
        var otherUser = UserBuilder.New().Build();
        DbContext.Users.Add(otherUser);
        var otherOrg = OrganizationBuilder.New().WithSlug("other-user-org-" + Guid.NewGuid().ToString("N")[..6]).Build();
        DbContext.Organizations.Add(otherOrg);
        var otherMembership = OrganizationMemberBuilder.New()
            .WithOrganization(otherOrg.Id)
            .WithUser(otherUser.Id)
            .Build();
        DbContext.OrganizationMembers.Add(otherMembership);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListOrganizationsQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotContain(o => o.Id == otherOrg.Id);
    }
}
