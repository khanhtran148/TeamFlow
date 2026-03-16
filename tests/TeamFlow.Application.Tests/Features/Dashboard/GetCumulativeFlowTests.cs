using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.GetCumulativeFlow;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Dashboard;

[Collection("Dashboard")]
public sealed class GetCumulativeFlowTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidDateRange_ReturnsFlowData()
    {
        var project = await SeedProjectAsync();
        await SeedWorkItemAsync(project.Id, b => b.WithStatus(WorkItemStatus.Done));
        await SeedWorkItemAsync(project.Id, b => b.WithStatus(WorkItemStatus.InProgress));
        await DbContext.SaveChangesAsync();

        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 12, 31);

        var result = await Sender.Send(new GetCumulativeFlowQuery(project.Id, from, to));

        result.IsSuccess.Should().BeTrue();
        result.Value.DataPoints.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_EmptyProject_ReturnsEmptyDataPoints()
    {
        var project = await SeedProjectAsync();

        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);

        var result = await Sender.Send(new GetCumulativeFlowQuery(project.Id, from, to));

        result.IsSuccess.Should().BeTrue();
        result.Value.DataPoints.Should().BeEmpty();
    }
}

[Collection("Dashboard")]
public sealed class GetCumulativeFlowDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();

        var query = new GetCumulativeFlowQuery(project.Id, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));
        var result = await Sender.Send(query);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
