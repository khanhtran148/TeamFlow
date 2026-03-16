using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases.GetReleaseDetail;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

public sealed class GetReleaseDetailTests
{
    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public GetReleaseDetailTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private GetReleaseDetailHandler CreateHandler() =>
        new(_releaseRepo, _workItemRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_MixedStatuses_ReturnsCorrectProgressCounts()
    {
        var release = ReleaseBuilder.New().WithProject(ProjectId).Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var doneItem = WorkItemBuilder.New().WithProject(ProjectId).WithStatus(WorkItemStatus.Done).WithEstimation(5).Build();
        var inProgressItem = WorkItemBuilder.New().WithProject(ProjectId).WithStatus(WorkItemStatus.InProgress).WithEstimation(3).Build();
        var todoItem = WorkItemBuilder.New().WithProject(ProjectId).WithStatus(WorkItemStatus.ToDo).WithEstimation(8).Build();
        var inReviewItem = WorkItemBuilder.New().WithProject(ProjectId).WithStatus(WorkItemStatus.InReview).WithEstimation(2).Build();

        _workItemRepo.GetByReleaseIdAsync(release.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { doneItem, inProgressItem, todoItem, inReviewItem }.AsEnumerable());

        var result = await CreateHandler().Handle(new GetReleaseDetailQuery(release.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Progress.TotalItems.Should().Be(4);
        result.Value.Progress.DoneItems.Should().Be(1);
        result.Value.Progress.InProgressItems.Should().Be(2);
        result.Value.Progress.ToDoItems.Should().Be(1);
        result.Value.Progress.TotalPoints.Should().Be(18);
        result.Value.Progress.DonePoints.Should().Be(5);
        result.Value.Progress.InProgressPoints.Should().Be(5);
        result.Value.Progress.ToDoPoints.Should().Be(8);
    }

    [Fact]
    public async Task Handle_ItemsGroupedByAssignee_ReturnsGroupedView()
    {
        var release = ReleaseBuilder.New().WithProject(ProjectId).Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var assignee1 = new User { Name = "Alice" };
        var assignee2 = new User { Name = "Bob" };
        var item1 = WorkItemBuilder.New().WithProject(ProjectId).WithStatus(WorkItemStatus.Done).Build();
        item1.Assignee = assignee1;
        item1.AssigneeId = assignee1.Id;
        var item2 = WorkItemBuilder.New().WithProject(ProjectId).WithStatus(WorkItemStatus.InProgress).Build();
        item2.Assignee = assignee1;
        item2.AssigneeId = assignee1.Id;
        var item3 = WorkItemBuilder.New().WithProject(ProjectId).WithStatus(WorkItemStatus.Done).Build();
        item3.Assignee = assignee2;
        item3.AssigneeId = assignee2.Id;

        _workItemRepo.GetByReleaseIdAsync(release.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { item1, item2, item3 }.AsEnumerable());

        var result = await CreateHandler().Handle(new GetReleaseDetailQuery(release.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByAssignee.Should().HaveCount(2);
        var aliceGroup = result.Value.ByAssignee.First(g => g.GroupName == "Alice");
        aliceGroup.ItemCount.Should().Be(2);
        aliceGroup.DoneCount.Should().Be(1);
        var bobGroup = result.Value.ByAssignee.First(g => g.GroupName == "Bob");
        bobGroup.ItemCount.Should().Be(1);
        bobGroup.DoneCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_PastReleaseDate_IsOverdueTrue()
    {
        var release = ReleaseBuilder.New()
            .WithProject(ProjectId)
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
            .WithStatus(ReleaseStatus.Unreleased)
            .Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        _workItemRepo.GetByReleaseIdAsync(release.Id, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<WorkItem>());

        var result = await CreateHandler().Handle(new GetReleaseDetailQuery(release.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReleasedStatus_IsOverdueFalse()
    {
        var release = ReleaseBuilder.New()
            .WithProject(ProjectId)
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)))
            .Released()
            .Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        _workItemRepo.GetByReleaseIdAsync(release.Id, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<WorkItem>());

        var result = await CreateHandler().Handle(new GetReleaseDetailQuery(release.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_FutureReleaseDate_IsOverdueFalse()
    {
        var release = ReleaseBuilder.New()
            .WithProject(ProjectId)
            .WithReleaseDate(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)))
            .WithStatus(ReleaseStatus.Unreleased)
            .Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        _workItemRepo.GetByReleaseIdAsync(release.Id, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<WorkItem>());

        var result = await CreateHandler().Handle(new GetReleaseDetailQuery(release.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsFailure()
    {
        _releaseRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var result = await CreateHandler().Handle(new GetReleaseDetailQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var release = ReleaseBuilder.New().WithProject(ProjectId).Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        _permissions.HasPermissionAsync(UserId, ProjectId, Permission.Release_View, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(new GetReleaseDetailQuery(release.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
