using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Reports.GetTeamHealthSummary;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Reports;

[Collection("Reports")]
public sealed class GetTeamHealthSummaryTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_SummaryExists_ReturnsLatestSummary()
    {
        var project = await SeedProjectAsync();
        var summary = new TeamHealthSummary
        {
            ProjectId = project.Id,
            PeriodStart = new DateOnly(2026, 3, 1),
            PeriodEnd = new DateOnly(2026, 3, 14),
            SummaryData = JsonDocument.Parse("""{"morale":4.2,"velocity_trend":"up"}"""),
        };
        DbContext.TeamHealthSummaries.Add(summary);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetTeamHealthSummaryQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.ProjectId.Should().Be(project.Id);
        result.Value.PeriodStart.Should().Be(new DateOnly(2026, 3, 1));
        result.Value.PeriodEnd.Should().Be(new DateOnly(2026, 3, 14));
    }

    [Fact]
    public async Task Handle_NoSummary_ReturnsNotFound()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new GetTeamHealthSummaryQuery(project.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}

[Collection("Reports")]
public sealed class GetTeamHealthSummaryDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new GetTeamHealthSummaryQuery(project.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
