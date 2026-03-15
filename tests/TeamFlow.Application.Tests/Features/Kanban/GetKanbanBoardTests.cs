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
        _linkRepo.GetBlockersForItemAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<WorkItemLink>());
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
        var blocker = WorkItemBuilder.New().WithStatus(WorkItemStatus.InProgress).Build();
        var link = new WorkItemLink
        {
            SourceId = blocker.Id,
            TargetId = item.Id,
            LinkType = LinkType.Blocks,
            Scope = LinkScope.SameProject,
            CreatedById = Guid.NewGuid(),
            Source = blocker
        };
        _workItemRepo.GetKanbanItemsAsync(
            projectId, null, null, null, null, null, Arg.Any<CancellationToken>())
            .Returns(new[] { item });
        _linkRepo.GetBlockersForItemAsync(item.Id, Arg.Any<CancellationToken>()).Returns(new[] { link });

        var query = new GetKanbanBoardQuery(projectId, null, null, null, null, null, null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var kanbanItem = result.Value.Columns.First(c => c.Status == WorkItemStatus.ToDo).Items.First();
        kanbanItem.IsBlocked.Should().BeTrue();
    }
}
