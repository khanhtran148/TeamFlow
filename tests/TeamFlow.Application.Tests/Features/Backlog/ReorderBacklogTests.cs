using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Backlog.ReorderBacklog;

namespace TeamFlow.Application.Tests.Features.Backlog;

public sealed class ReorderBacklogTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public ReorderBacklogTests()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private ReorderBacklogHandler CreateHandler() =>
        new(_workItemRepo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ValidReorder_UpdatesSortOrders()
    {
        var projectId = Guid.NewGuid();
        var items = new[]
        {
            new WorkItemSortOrder(Guid.NewGuid(), 1),
            new WorkItemSortOrder(Guid.NewGuid(), 2)
        };

        var result = await CreateHandler().Handle(
            new ReorderBacklogCommand(projectId, items), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _workItemRepo.Received(2).UpdateSortOrderAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
