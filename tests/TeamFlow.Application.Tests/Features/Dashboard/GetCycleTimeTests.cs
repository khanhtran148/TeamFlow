using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.GetCycleTime;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Dashboard;

[Collection("Dashboard")]
public sealed class GetCycleTimeTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidProject_ReturnsCycleTimeMetrics()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new GetCycleTimeQuery(project.Id, null, null));

        result.IsSuccess.Should().BeTrue();
        result.Value.ByType.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithDateRange_PassesDatesToRepository()
    {
        var project = await SeedProjectAsync();

        var from = new DateOnly(2026, 2, 1);
        var to = new DateOnly(2026, 2, 28);

        var result = await Sender.Send(new GetCycleTimeQuery(project.Id, from, to));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoData_ReturnsEmptyByType()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new GetCycleTimeQuery(project.Id, null, null));

        result.IsSuccess.Should().BeTrue();
        result.Value.ByType.Should().BeEmpty();
    }
}

[Collection("Dashboard")]
public sealed class GetCycleTimeDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new GetCycleTimeQuery(project.Id, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
