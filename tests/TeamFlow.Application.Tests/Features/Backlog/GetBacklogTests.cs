using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Backlog.GetBacklog;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Backlog;

public sealed class GetBacklogTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IWorkItemLinkRepository _linkRepo = Substitute.For<IWorkItemLinkRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public GetBacklogTests()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _linkRepo.GetBlockedItemIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid>());
    }

    private GetBacklogHandler CreateHandler() =>
        new(_workItemRepo, _linkRepo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ReturnsPagedItems()
    {
        var projectId = Guid.NewGuid();
        var items = new List<WorkItem>
        {
            WorkItemBuilder.New().WithProject(projectId).WithType(WorkItemType.Epic).Build(),
            WorkItemBuilder.New().WithProject(projectId).WithType(WorkItemType.UserStory).Build()
        };
        _workItemRepo.GetBacklogPagedAsync(
            projectId, null, null, null, null, null, null, null, null, 1, 20,
            Arg.Any<CancellationToken>())
            .Returns((items, 2));

        var query = new GetBacklogQuery(projectId, null, null, null, null, null, null, null, null, 1, 20);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_BlockedItem_FlaggedCorrectly()
    {
        var projectId = Guid.NewGuid();
        var item = WorkItemBuilder.New().WithProject(projectId).Build();

        _workItemRepo.GetBacklogPagedAsync(
            projectId, null, null, null, null, null, null, null, null, 1, 20,
            Arg.Any<CancellationToken>())
            .Returns((new[] { item }, 1));
        _linkRepo.GetBlockedItemIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid> { item.Id });

        var query = new GetBacklogQuery(projectId, null, null, null, null, null, null, null, null, 1, 20);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.First().IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NotBlockedItem_NotFlagged()
    {
        var projectId = Guid.NewGuid();
        var item = WorkItemBuilder.New().WithProject(projectId).Build();

        _workItemRepo.GetBacklogPagedAsync(
            projectId, null, null, null, null, null, null, null, null, 1, 20,
            Arg.Any<CancellationToken>())
            .Returns((new[] { item }, 1));
        // Default: empty set (no blocked items)

        var query = new GetBacklogQuery(projectId, null, null, null, null, null, null, null, null, 1, 20);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.First().IsBlocked.Should().BeFalse();
    }
}
