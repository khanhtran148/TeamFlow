using FluentAssertions;
using TeamFlow.Application.Features.Releases.ShipRelease;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

[Collection("Releases")]
public sealed class ShipReleaseTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_NoOpenItems_ShipsImmediately()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.Add(release);
        await SeedWorkItemAsync(project.Id, b => b.WithRelease(release.Id).WithStatus(WorkItemStatus.Done));
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ShipReleaseCommand(release.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Shipped.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.Releases.FindAsync(release.Id);
        updated!.Status.Should().Be(ReleaseStatus.Released);
        updated.NotesLocked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OpenItemsWithoutConfirm_Returns409WithList()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.Add(release);
        await SeedWorkItemAsync(project.Id, b => b
            .WithRelease(release.Id)
            .WithStatus(WorkItemStatus.InProgress)
            .WithTitle("Open task"));
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ShipReleaseCommand(release.Id, false));

        result.IsSuccess.Should().BeTrue();
        result.Value.Shipped.Should().BeFalse();
        result.Value.IncompleteItems.Should().HaveCount(1);
        result.Value.IncompleteItems![0].Title.Should().Be("Open task");
    }

    [Fact]
    public async Task Handle_OpenItemsWithConfirm_Ships()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Build();
        DbContext.Releases.Add(release);
        await SeedWorkItemAsync(project.Id, b => b.WithRelease(release.Id).WithStatus(WorkItemStatus.InProgress));
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ShipReleaseCommand(release.Id, true));

        result.IsSuccess.Should().BeTrue();
        result.Value.Shipped.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var updated = await DbContext.Releases.FindAsync(release.Id);
        updated!.Status.Should().Be(ReleaseStatus.Released);
    }

    [Fact]
    public async Task Handle_AlreadyReleased_ReturnsFailure()
    {
        var project = await SeedProjectAsync();
        var release = ReleaseBuilder.New().WithProject(project.Id).Released().Build();
        DbContext.Releases.Add(release);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ShipReleaseCommand(release.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already been shipped");
    }
}
