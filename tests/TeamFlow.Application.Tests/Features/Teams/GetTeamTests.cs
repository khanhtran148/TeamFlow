using FluentAssertions;
using TeamFlow.Application.Features.Teams.GetTeam;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

[Collection("Projects")]
public sealed class GetTeamTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ExistingTeam_ReturnsTeamDto()
    {
        var team = TeamBuilder.New()
            .WithOrganization(SeedOrgId)
            .WithName("Alpha Team")
            .WithDescription("The best team")
            .Build();
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetTeamQuery(team.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Alpha Team");
        result.Value.Description.Should().Be("The best team");
    }

    [Fact]
    public async Task Handle_NonExistentTeam_ReturnsFailure()
    {
        var result = await Sender.Send(new GetTeamQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
