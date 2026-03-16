using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.GetDashboardSummary;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Dashboard;

[Collection("Dashboard")]
public sealed class GetDashboardSummaryTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidProject_ReturnsSummary()
    {
        var project = await SeedProjectAsync();
        await SeedWorkItemAsync(project.Id);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetDashboardSummaryQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalItems.Should().BeGreaterThanOrEqualTo(0);
        result.Value.CompletionPct.Should().BeGreaterThanOrEqualTo(0);
    }
}

[Collection("Dashboard")]
public sealed class GetDashboardSummaryDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new GetDashboardSummaryQuery(project.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
