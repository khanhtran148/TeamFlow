using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases.UnassignItem;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

[Collection("Releases")]
public sealed class UnassignItemTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ValidCommand_UnassignsItemFromRelease()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.Add(release);
        var workItem = await SeedWorkItemAsync(project.Id, b => b.WithRelease(release.Id));
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new UnassignItemFromReleaseCommand(release.Id, workItem.Id));

        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.WorkItems.FindAsync(workItem.Id);
        updated!.ReleaseId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReleaseNotFound_ReturnsFailure()
    {
        var result = await Sender.Send(new UnassignItemFromReleaseCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Release not found");
    }

    [Fact]
    public async Task Handle_WorkItemNotInRelease_ReturnsFailure()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.Add(release);
        var workItem = await SeedWorkItemAsync(project.Id); // no release assigned
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new UnassignItemFromReleaseCommand(release.Id, workItem.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not assigned");
    }
}

[Collection("Releases")]
public sealed class UnassignItemDeniedTests(PostgresCollectionFixture fixture)
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

        var result = await Sender.Send(new UnassignItemFromReleaseCommand(release.Id, Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
