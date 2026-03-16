using FluentAssertions;
using TeamFlow.Application.Features.WorkItems.AddLink;
using TeamFlow.Application.Features.Kanban.GetKanbanBoard;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Kanban;

[Collection("WorkItems")]
public sealed class GetKanbanBoardTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    [Fact]
    public async Task Handle_ItemsGroupedByStatus()
    {
        var project = await SeedProjectAsync();
        await SeedWorkItemAsync(project.Id, b => b.AsTask().WithStatus(WorkItemStatus.ToDo));
        await SeedWorkItemAsync(project.Id, b => b.AsTask().WithStatus(WorkItemStatus.InProgress));
        await SeedWorkItemAsync(project.Id, b => b.AsTask().WithStatus(WorkItemStatus.ToDo));

        var query = new GetKanbanBoardQuery(project.Id, null, null, null, null, null, null);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().HaveCount(4); // All 4 status columns

        var todoColumn = result.Value.Columns.First(c => c.Status == WorkItemStatus.ToDo);
        todoColumn.ItemCount.Should().Be(2);

        var inProgressColumn = result.Value.Columns.First(c => c.Status == WorkItemStatus.InProgress);
        inProgressColumn.ItemCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_BlockedItem_FlaggedInBoard()
    {
        var project = await SeedProjectAsync();
        var item = await SeedWorkItemAsync(project.Id, b => b.AsTask().WithStatus(WorkItemStatus.ToDo));
        var blocker = await SeedWorkItemAsync(project.Id, b => b.AsTask().WithStatus(WorkItemStatus.InProgress));

        await Sender.Send(new AddWorkItemLinkCommand(blocker.Id, item.Id, LinkType.Blocks));
        DbContext.ChangeTracker.Clear();

        var query = new GetKanbanBoardQuery(project.Id, null, null, null, null, null, null);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        var kanbanItem = result.Value.Columns.First(c => c.Status == WorkItemStatus.ToDo).Items
            .FirstOrDefault(i => i.Id == item.Id);
        kanbanItem.Should().NotBeNull();
        kanbanItem!.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoItems_ReturnsEmptyColumns()
    {
        var project = await SeedProjectAsync();

        var query = new GetKanbanBoardQuery(project.Id, null, null, null, null, null, null);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().HaveCount(4);
        result.Value.Columns.Should().AllSatisfy(c => c.ItemCount.Should().Be(0));
    }

    [Fact]
    public async Task Handle_SwimlaneAssignee_GroupsByAssignee()
    {
        var project = await SeedProjectAsync();

        var alice = UserBuilder.New().WithName("Alice").Build();
        var bob = UserBuilder.New().WithName("Bob").Build();
        DbContext.Users.AddRange(alice, bob);
        await DbContext.SaveChangesAsync();

        await SeedWorkItemAsync(project.Id, b => b.AsTask().WithAssignee(alice.Id).WithStatus(WorkItemStatus.ToDo));
        await SeedWorkItemAsync(project.Id, b => b.AsTask().WithAssignee(bob.Id).WithStatus(WorkItemStatus.InProgress));
        await SeedWorkItemAsync(project.Id, b => b.AsTask().WithAssignee(alice.Id).WithStatus(WorkItemStatus.InProgress));

        DbContext.ChangeTracker.Clear();

        var query = new GetKanbanBoardQuery(project.Id, null, null, null, null, null, "assignee");
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Swimlanes.Should().NotBeNull();
        var swimlanes = result.Value.Swimlanes!.ToList();
        swimlanes.Should().HaveCount(2);
        swimlanes.Should().Contain(s => s.Label == "Alice");
        swimlanes.Should().Contain(s => s.Label == "Bob");

        var aliceLane = swimlanes.First(s => s.Label == "Alice");
        aliceLane.Columns.Sum(c => c.ItemCount).Should().Be(2);
    }

    [Fact]
    public async Task Handle_SwimlaneEpic_GroupsByParent()
    {
        var project = await SeedProjectAsync();

        var epic = await SeedWorkItemAsync(project.Id, b => b.AsEpic().WithTitle("Epic One"));
        var story = await SeedWorkItemAsync(project.Id, b => b.WithType(WorkItemType.UserStory).WithParent(epic.Id).WithStatus(WorkItemStatus.ToDo));
        var task = await SeedWorkItemAsync(project.Id, b => b.AsTask().WithStatus(WorkItemStatus.InProgress));

        DbContext.ChangeTracker.Clear();

        var query = new GetKanbanBoardQuery(project.Id, null, null, null, null, null, "epic");
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Swimlanes.Should().NotBeNull();
        var swimlanes = result.Value.Swimlanes!.ToList();
        swimlanes.Should().HaveCount(2);
        swimlanes.Should().Contain(s => s.Label == "Epic One");
        swimlanes.Should().Contain(s => s.Label == "No Epic");
    }

    [Fact]
    public async Task Handle_NoSwimlane_ReturnsNullSwimlanes()
    {
        var project = await SeedProjectAsync();
        await SeedWorkItemAsync(project.Id, b => b.AsTask().WithStatus(WorkItemStatus.ToDo));

        var query = new GetKanbanBoardQuery(project.Id, null, null, null, null, null, null);
        var result = await Sender.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Swimlanes.Should().BeNull();
        result.Value.Columns.Should().HaveCount(4);
    }
}
