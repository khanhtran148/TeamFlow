using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Teams.DeleteTeam;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Teams;

[Collection("Projects")]
public sealed class DeleteTeamTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ExistingTeam_DeletesSuccessfully()
    {
        var team = TeamBuilder.New().WithOrganization(SeedOrgId).Build();
        DbContext.Teams.Add(team);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new DeleteTeamCommand(team.Id));

        result.IsSuccess.Should().BeTrue();
        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.Teams.FindAsync(team.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Handle_TeamNotFound_ReturnsFailure()
    {
        var result = await Sender.Send(new DeleteTeamCommand(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}

[Collection("Projects")]
public sealed class DeleteTeamForbiddenTests(PostgresCollectionFixture fixture)
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

        var result = await Sender.Send(new DeleteTeamCommand(team.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
