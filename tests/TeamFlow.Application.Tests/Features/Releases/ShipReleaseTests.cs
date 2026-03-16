using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases.ShipRelease;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

public sealed class ShipReleaseTests
{
    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public ShipReleaseTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _releaseRepo.UpdateAsync(Arg.Any<Release>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Release>());
    }

    private ShipReleaseHandler CreateHandler() =>
        new(_releaseRepo, _workItemRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_NoOpenItems_ShipsImmediately()
    {
        var release = ReleaseBuilder.New().WithProject(ProjectId).Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var doneItem = WorkItemBuilder.New().WithStatus(WorkItemStatus.Done).Build();
        _workItemRepo.GetByReleaseIdAsync(release.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { doneItem }.AsEnumerable());

        var result = await CreateHandler().Handle(
            new ShipReleaseCommand(release.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Shipped.Should().BeTrue();
        release.Status.Should().Be(ReleaseStatus.Released);
        release.NotesLocked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OpenItemsWithoutConfirm_Returns409WithList()
    {
        var release = ReleaseBuilder.New().WithProject(ProjectId).Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var openItem = WorkItemBuilder.New().WithStatus(WorkItemStatus.InProgress).WithTitle("Open task").Build();
        _workItemRepo.GetByReleaseIdAsync(release.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { openItem }.AsEnumerable());

        var result = await CreateHandler().Handle(
            new ShipReleaseCommand(release.Id, false), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Shipped.Should().BeFalse();
        result.Value.IncompleteItems.Should().HaveCount(1);
        result.Value.IncompleteItems![0].Title.Should().Be("Open task");
    }

    [Fact]
    public async Task Handle_OpenItemsWithConfirm_Ships()
    {
        var release = ReleaseBuilder.New().WithProject(ProjectId).Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var openItem = WorkItemBuilder.New().WithStatus(WorkItemStatus.InProgress).Build();
        _workItemRepo.GetByReleaseIdAsync(release.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { openItem }.AsEnumerable());

        var result = await CreateHandler().Handle(
            new ShipReleaseCommand(release.Id, true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Shipped.Should().BeTrue();
        release.Status.Should().Be(ReleaseStatus.Released);
    }

    [Fact]
    public async Task Handle_AlreadyReleased_ReturnsFailure()
    {
        var release = ReleaseBuilder.New().WithProject(ProjectId).Released().Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);

        var result = await CreateHandler().Handle(
            new ShipReleaseCommand(release.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already been shipped");
    }
}
