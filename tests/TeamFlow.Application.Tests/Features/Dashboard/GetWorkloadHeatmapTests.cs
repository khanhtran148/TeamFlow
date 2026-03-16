using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.GetWorkloadHeatmap;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Dashboard;

[Collection("Dashboard")]
public sealed class GetWorkloadHeatmapTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidProject_ReturnsPerMemberWorkload()
    {
        var project = await SeedProjectAsync();
        await SeedWorkItemAsync(project.Id, b => b.WithAssignee(SeedUserId));
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetWorkloadHeatmapQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Members.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_EmptyProject_ReturnsEmptyMembers()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new GetWorkloadHeatmapQuery(project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Members.Should().BeEmpty();
    }
}

[Collection("Dashboard")]
public sealed class GetWorkloadHeatmapDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();

        var result = await Sender.Send(new GetWorkloadHeatmapQuery(project.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
