using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Reports.GetSprintReport;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Reports;

[Collection("Reports")]
public sealed class GetSprintReportTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ReportExists_ReturnsReport()
    {
        var project = await SeedProjectAsync();
        var sprint = SprintBuilder.New().WithProject(project.Id).Build();
        DbContext.Sprints.Add(sprint);
        var report = new SprintReport
        {
            SprintId = sprint.Id,
            ProjectId = project.Id,
            ReportData = JsonDocument.Parse("""{"velocity":35}"""),
            GeneratedBy = "System"
        };
        DbContext.SprintReports.Add(report);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetSprintReportQuery(sprint.Id, project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.SprintId.Should().Be(sprint.Id);
    }

    [Fact]
    public async Task Handle_ReportNotFound_ReturnsNotFound()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new GetSprintReportQuery(Guid.NewGuid(), project.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}

[Collection("Reports")]
public sealed class GetSprintReportDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new GetSprintReportQuery(Guid.NewGuid(), project.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
