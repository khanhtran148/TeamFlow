using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams.UpdateTeam;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

[Collection("Projects")]
public sealed class UpdateTeamTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidCommand_UpdatesTeam()
    {
        var team = TeamBuilder.New().WithOrganization(SeedOrgId).WithName("Old Name").Build();
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        var cmd = new UpdateTeamCommand(team.Id, "New Name", "New Desc");
        var result = await Sender.Send(cmd);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task Handle_TeamNotFound_ReturnsFailure()
    {
        var cmd = new UpdateTeamCommand(Guid.NewGuid(), "Name", null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}

[Collection("Projects")]
public sealed class UpdateTeamForbiddenTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var team = TeamBuilder.New().WithOrganization(SeedOrgId).Build();
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        var cmd = new UpdateTeamCommand(team.Id, "Name", null);
        var result = await Sender.Send(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
