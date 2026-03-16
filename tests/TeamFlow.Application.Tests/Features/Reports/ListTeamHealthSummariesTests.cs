using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Reports.ListTeamHealthSummaries;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Reports;

[Collection("Reports")]
public sealed class ListTeamHealthSummariesTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_HasSummaries_ReturnsPaginatedList()
    {
        var project = await SeedProjectAsync();
        DbContext.TeamHealthSummaries.Add(new TeamHealthSummary
        {
            ProjectId = project.Id,
            PeriodStart = new DateOnly(2026, 3, 1),
            PeriodEnd = new DateOnly(2026, 3, 14),
            SummaryData = JsonDocument.Parse("""{"morale":8}""")
        });
        DbContext.TeamHealthSummaries.Add(new TeamHealthSummary
        {
            ProjectId = project.Id,
            PeriodStart = new DateOnly(2026, 3, 15),
            PeriodEnd = new DateOnly(2026, 3, 28),
            SummaryData = JsonDocument.Parse("""{"morale":7}""")
        });
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListTeamHealthSummariesQuery(project.Id, 1, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Page.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NoSummaries_ReturnsEmptyPagedResult()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new ListTeamHealthSummariesQuery(project.Id, 1, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }
}

[Collection("Reports")]
public sealed class ListTeamHealthSummariesDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new ListTeamHealthSummariesQuery(project.Id, 1, 10));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
