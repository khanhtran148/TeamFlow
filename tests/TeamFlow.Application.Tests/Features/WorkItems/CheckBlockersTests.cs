using FluentAssertions;
using TeamFlow.Application.Features.WorkItems.AddLink;
using TeamFlow.Application.Features.WorkItems.CheckBlockers;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

[Collection("WorkItems")]
public sealed class CheckBlockersTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_NoBlockers_ReturnsEmptyList()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        var result = await Sender.Send(new CheckBlockersQuery(item.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.HasUnresolvedBlockers.Should().BeFalse();
        result.Value.Blockers.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DoneBlocker_NotReturned()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.AsTask());
        var blocker = await SeedWorkItemAsync(project.Id, b => b.AsTask().WithStatus(WorkItemStatus.Done));

        // blocker Blocks item
        await Sender.Send(new AddWorkItemLinkCommand(blocker.Id, item.Id, LinkType.Blocks));
        DbContext.ChangeTracker.Clear();

        var result = await Sender.Send(new CheckBlockersQuery(item.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.HasUnresolvedBlockers.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ActiveBlocker_ReturnedInList()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.AsTask());
        var blockerTitle = "Blocker Task " + Guid.NewGuid();
        var blocker = await SeedWorkItemAsync(project.Id,
            b => b.AsTask().WithStatus(WorkItemStatus.InProgress).WithTitle(blockerTitle));

        // blocker Blocks item
        await Sender.Send(new AddWorkItemLinkCommand(blocker.Id, item.Id, LinkType.Blocks));
        DbContext.ChangeTracker.Clear();

        var result = await Sender.Send(new CheckBlockersQuery(item.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.HasUnresolvedBlockers.Should().BeTrue();
        result.Value.Blockers.Should().HaveCount(1);
        result.Value.Blockers.First().Title.Should().Be(blockerTitle);
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        var result = await Sender.Send(new CheckBlockersQuery(Guid.NewGuid()));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
