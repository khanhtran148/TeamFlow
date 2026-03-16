using FluentAssertions;
using TeamFlow.Application.Features.WorkItems.AddLink;
using TeamFlow.Application.Features.Backlog.GetBacklog;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Backlog;

[Collection("WorkItems")]
public sealed class GetBacklogTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ReturnsPagedItems()
    {
        var project = await SeedProjectAsync();
        await SeedWorkItemAsync(project.Id, b => b.WithType(WorkItemType.Epic));
        await SeedWorkItemAsync(project.Id, b => b.WithType(WorkItemType.UserStory));

        var query = new GetBacklogQuery(project.Id, null, null, null, null, null, null, null, null, null, 1, 20);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_BlockedItem_FlaggedCorrectly()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.AsTask());
        var blocker = await SeedWorkItemAsync(project.Id, b => b.AsTask().WithStatus(WorkItemStatus.InProgress));

        // blocker Blocks item
        await Sender.Send(new AddWorkItemLinkCommand(blocker.Id, item.Id, LinkType.Blocks));
        DbContext.ChangeTracker.Clear();

        var query = new GetBacklogQuery(project.Id, null, null, null, null, null, null, null, null, null, 1, 20);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        var blockedItem = result.Value.Items.FirstOrDefault(i => i.Id == item.Id);
        blockedItem.Should().NotBeNull();
        blockedItem!.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NotBlockedItem_NotFlagged()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        var query = new GetBacklogQuery(project.Id, null, null, null, null, null, null, null, null, null, 1, 20);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.First().IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_IsReadyFilter_ReturnsOnlyReadyItems()
    {
        var project = await SeedProjectAsync();
        var readyItem = await SeedWorkItemAsync(project.Id, b => b.AsTask());
        var notReadyItem = await SeedWorkItemAsync(project.Id, b => b.AsTask());

        // Mark one as ready
        readyItem.IsReadyForSprint = true;
        DbContext.WorkItems.Update(readyItem);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        var query = new GetBacklogQuery(project.Id, null, null, null, null, null, null, null, null, true, 1, 20);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().Id.Should().Be(readyItem.Id);
    }
}
