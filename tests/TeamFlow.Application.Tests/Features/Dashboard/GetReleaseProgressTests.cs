using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.GetReleaseProgress;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Dashboard;

[Collection("Dashboard")]
public sealed class GetReleaseProgressTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidRelease_ReturnsProgressCounts()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetReleaseProgressQuery(release.Id, project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.DoneCount.Should().BeGreaterThanOrEqualTo(0);
        result.Value.TotalPoints.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Handle_EmptyRelease_ReturnsZeroCounts()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetReleaseProgressQuery(release.Id, project.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.DoneCount.Should().Be(0);
        result.Value.TotalPoints.Should().Be(0m);
    }
}

[Collection("Dashboard")]
public sealed class GetReleaseProgressDeniedTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    protected override void ConfigureServices(IServiceCollection services)
        => services.AddScoped<IPermissionChecker, AlwaysDenyTestPermissionChecker>();

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetReleaseProgressQuery(release.Id, project.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
