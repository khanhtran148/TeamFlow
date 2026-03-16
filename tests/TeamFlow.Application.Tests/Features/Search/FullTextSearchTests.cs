using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Search.FullTextSearch;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Search;

public sealed class FullTextSearchTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public FullTextSearchTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private FullTextSearchHandler CreateHandler() =>
        new(_workItemRepo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ValidQuery_ReturnsPagedResults()
    {
        var items = new[] { WorkItemBuilder.New().WithProject(ProjectId).Build() };
        _workItemRepo.GetBacklogPagedAsync(
            ProjectId, Arg.Any<WorkItemStatus?>(), Arg.Any<Priority?>(),
            Arg.Any<Guid?>(), Arg.Any<WorkItemType?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(),
            Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<bool?>(),
            1, 20, Arg.Any<CancellationToken>())
            .Returns((items.AsEnumerable(), 1));

        var query = new FullTextSearchQuery(ProjectId, "test", null, null, null, null, null, null, null, null, 1, 20);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NoResults_ReturnsEmptyPage()
    {
        _workItemRepo.GetBacklogPagedAsync(
            ProjectId, Arg.Any<WorkItemStatus?>(), Arg.Any<Priority?>(),
            Arg.Any<Guid?>(), Arg.Any<WorkItemType?>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(),
            Arg.Any<bool?>(), Arg.Any<string?>(), Arg.Any<bool?>(),
            1, 20, Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<WorkItem>(), 0));

        var query = new FullTextSearchQuery(ProjectId, "nonexistent", null, null, null, null, null, null, null, null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var query = new FullTextSearchQuery(ProjectId, "test", null, null, null, null, null, null, null, null);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
