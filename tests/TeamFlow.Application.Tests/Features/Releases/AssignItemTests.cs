using FluentAssertions;
using TeamFlow.Application.Features.Releases.AssignItem;
using TeamFlow.Application.Features.Releases.UnassignItem;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

[Collection("Releases")]
public sealed class AssignItemTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Assign_ValidItem_SetsReleaseId()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.Add(release);
        var item = await SeedWorkItemAsync(project.Id);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AssignItemToReleaseCommand(release.Id, item.Id));

        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.WorkItems.FindAsync(item.Id);
        updated!.ReleaseId.Should().Be(release.Id);
    }

    [Fact]
    public async Task Assign_ItemInAnotherRelease_ReturnsError()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        var otherRelease = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.AddRange(release, otherRelease);
        var item = await SeedWorkItemAsync(project.Id, b => b.WithRelease(otherRelease.Id));
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new AssignItemToReleaseCommand(release.Id, item.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("another release");
    }

    [Fact]
    public async Task Unassign_AssignedItem_ClearsReleaseId()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.Add(release);
        var item = await SeedWorkItemAsync(project.Id, b => b.WithRelease(release.Id));
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new UnassignItemFromReleaseCommand(release.Id, item.Id));

        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.WorkItems.FindAsync(item.Id);
        updated!.ReleaseId.Should().BeNull();
    }
}
