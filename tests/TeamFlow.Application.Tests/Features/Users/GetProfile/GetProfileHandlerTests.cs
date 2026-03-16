using FluentAssertions;
using TeamFlow.Application.Features.Users.GetProfile;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Users.GetProfile;

[Collection("Auth")]
public sealed class GetProfileHandlerTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_AuthenticatedUser_ReturnsFullProfile()
    {
        var org = OrganizationBuilder.New().WithName("Acme Corp").Build();
        DbContext.Set<Organization>().Add(org);
        await DbContext.SaveChangesAsync();

        DbContext.Set<OrganizationMember>().Add(new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = SeedUserId,
            Role = OrgRole.Admin
        });

        var team = TeamBuilder.New().WithOrganization(org.Id).WithName("Backend Squad").Build();
        DbContext.Set<Team>().Add(team);
        await DbContext.SaveChangesAsync();

        DbContext.Set<TeamMember>().Add(new TeamMember
        {
            TeamId = team.Id,
            UserId = SeedUserId,
            Role = ProjectRole.Developer
        });
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetProfileQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(SeedUserId);
        result.Value.Email.Should().Be("test@teamflow.dev");
        result.Value.Name.Should().Be("Test User");
        result.Value.SystemRole.Should().Be("User");
        result.Value.Organizations.Should().Contain(o => o.OrgName == "Acme Corp" && o.Role == "Admin");
        result.Value.Teams.Should().Contain(t => t.TeamName == "Backend Squad" && t.Role == "Developer");
    }

    [Fact]
    public async Task Handle_UserWithNoOrgsOrTeams_ReturnsEmptyCollections()
    {
        var result = await Sender.Send(new GetProfileQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Organizations.Should().BeEmpty();
        result.Value.Teams.Should().BeEmpty();
    }
}
