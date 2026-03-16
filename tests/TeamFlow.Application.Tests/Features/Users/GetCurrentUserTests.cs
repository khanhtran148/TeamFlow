using FluentAssertions;
using TeamFlow.Application.Features.Users.GetCurrentUser;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Users;

[Collection("Auth")]
public sealed class GetCurrentUserTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidUser_ReturnsCurrentUserWithOrganizations()
    {
        var org = OrganizationBuilder.New().WithName("Org A").Build();
        DbContext.Set<Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        DbContext.Set<OrganizationMember>().Add(new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = SeedUserId,
            Role = OrgRole.Member
        });
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetCurrentUserQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(SeedUserId);
        result.Value.Email.Should().Be("test@teamflow.dev");
        result.Value.Name.Should().Be("Test User");
        result.Value.Organizations.Should().Contain(o => o.OrgName == "Org A");
    }

    [Fact]
    public async Task Handle_UserWithNoOrganizations_ReturnsEmptyOrgList()
    {
        var result = await Sender.Send(new GetCurrentUserQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Organizations.Should().BeEmpty();
    }
}
