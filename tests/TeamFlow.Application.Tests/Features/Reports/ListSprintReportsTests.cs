using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Reports.ListSprintReports;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Reports;

[Collection("Reports")]
public sealed class ListSprintReportsTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidProject_ReturnsPaginatedReports()
    {
        var project = await SeedProjectAsync();
        var sprint1 = SprintBuilder.New().WithProject(project.Id).Build();
        var sprint2 = SprintBuilder.New().WithProject(project.Id).Build();
        DbContext.Sprints.AddRange(sprint1, sprint2);
        DbContext.SprintReports.Add(new SprintReport
        {
            SprintId = sprint1.Id,
            ProjectId = project.Id,
            ReportData = JsonDocument.Parse("{}"),
            GeneratedBy = "System"
        });
        DbContext.SprintReports.Add(new SprintReport
        {
            SprintId = sprint2.Id,
            ProjectId = project.Id,
            ReportData = JsonDocument.Parse("{}"),
            GeneratedBy = "System"
        });
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ListSprintReportsQuery(project.Id, 1, 10));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Page.Should().Be(1);
    }

    [Fact]
    public async Task Handle_EmptyProject_ReturnsEmptyPage()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new ListSprintReportsQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }
}

[Collection("Reports")]
public sealed class ListSprintReportsDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new ListSprintReportsQuery(project.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
