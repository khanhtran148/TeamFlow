using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.GetVelocityChart;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Dashboard;

[Collection("Dashboard")]
public sealed class GetVelocityChartTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidProject_ReturnsVelocityData()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new GetVelocityChartQuery(project.Id, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value.Sprints.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_EmptyProject_ReturnsEmptySprints()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new GetVelocityChartQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Sprints.Should().BeEmpty();
    }
}

[Collection("Dashboard")]
public sealed class GetVelocityChartDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new GetVelocityChartQuery(project.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
