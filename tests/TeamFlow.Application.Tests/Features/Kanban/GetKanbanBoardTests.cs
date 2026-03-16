using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Kanban.GetKanbanBoard;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Kanban;

public sealed class GetKanbanBoardTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IWorkItemLinkRepository _linkRepo = Substitute.For<IWorkItemLinkRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public GetKanbanBoardTests()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _linkRepo.GetBlockedItemIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid>());
    }

    private GetKanbanBoardHandler CreateHandler() =>
        new(_workItemRepo, _linkRepo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ItemsGroupedByStatus()
    {
        var projectId = Guid.NewGuid();
        var items = new List<WorkItem>
        {
            WorkItemBuilder.New().WithProject(projectId).WithStatus(WorkItemStatus.ToDo).Build(),
            WorkItemBuilder.New().WithProject(projectId).WithStatus(WorkItemStatus.InProgress).Build(),
            WorkItemBuilder.New().WithProject(projectId).WithStatus(WorkItemStatus.ToDo).Build()
        };
        _workItemRepo.GetKanbanItemsAsync(
            projectId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(items);

        var query = new GetKanbanBoardQuery(projectId, null, null, null, null, null, null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

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
        var projectId = Guid.NewGuid();
        var item = WorkItemBuilder.New().WithProject(projectId).WithStatus(WorkItemStatus.ToDo).Build();

        _workItemRepo.GetKanbanItemsAsync(
            projectId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { item });
        _linkRepo.GetBlockedItemIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid> { item.Id });

        var query = new GetKanbanBoardQuery(projectId, null, null, null, null, null, null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var kanbanItem = result.Value.Columns.First(c => c.Status == WorkItemStatus.ToDo).Items.First();
        kanbanItem.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NoItems_ReturnsEmptyColumns()
    {
        var projectId = Guid.NewGuid();
        _workItemRepo.GetKanbanItemsAsync(
            projectId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<WorkItem>());

        var query = new GetKanbanBoardQuery(projectId, null, null, null, null, null, null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Columns.Should().HaveCount(4);
        result.Value.Columns.Should().AllSatisfy(c => c.ItemCount.Should().Be(0));
    }

    [Fact]
    public async Task Handle_SwimlaneAssignee_GroupsByAssignee()
    {
        var projectId = Guid.NewGuid();
        var assignee1Id = Guid.NewGuid();
        var assignee2Id = Guid.NewGuid();

        var item1 = WorkItemBuilder.New().WithProject(projectId).WithAssignee(assignee1Id).WithStatus(WorkItemStatus.ToDo).Build();
        item1.Assignee = new User { Name = "Alice" };

        var item2 = WorkItemBuilder.New().WithProject(projectId).WithAssignee(assignee2Id).WithStatus(WorkItemStatus.InProgress).Build();
        item2.Assignee = new User { Name = "Bob" };

        var item3 = WorkItemBuilder.New().WithProject(projectId).WithAssignee(assignee1Id).WithStatus(WorkItemStatus.InProgress).Build();
        item3.Assignee = new User { Name = "Alice" };

        var items = new List<WorkItem> { item1, item2, item3 };
        _workItemRepo.GetKanbanItemsAsync(
            projectId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(items);

        var query = new GetKanbanBoardQuery(projectId, null, null, null, null, null, "assignee");
        var result = await CreateHandler().Handle(query, CancellationToken.None);

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
        var projectId = Guid.NewGuid();
        var epicId = Guid.NewGuid();

        var item1 = WorkItemBuilder.New().WithProject(projectId).WithParent(epicId).WithStatus(WorkItemStatus.ToDo).Build();
        item1.Parent = new WorkItem { Title = "Epic One" };

        var item2 = WorkItemBuilder.New().WithProject(projectId).WithStatus(WorkItemStatus.InProgress).Build();

        var items = new List<WorkItem> { item1, item2 };
        _workItemRepo.GetKanbanItemsAsync(
            projectId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(items);

        var query = new GetKanbanBoardQuery(projectId, null, null, null, null, null, "epic");
        var result = await CreateHandler().Handle(query, CancellationToken.None);

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
        var projectId = Guid.NewGuid();
        var items = new List<WorkItem>
        {
            WorkItemBuilder.New().WithProject(projectId).WithStatus(WorkItemStatus.ToDo).Build()
        };
        _workItemRepo.GetKanbanItemsAsync(
            projectId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(items);

        var query = new GetKanbanBoardQuery(projectId, null, null, null, null, null, null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Swimlanes.Should().BeNull();
        result.Value.Columns.Should().HaveCount(4);
    }
}
