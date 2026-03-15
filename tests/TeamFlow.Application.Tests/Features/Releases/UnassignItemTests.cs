using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases.UnassignItem;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

public sealed class UnassignItemTests
{
    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public UnassignItemTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private UnassignItemFromReleaseHandler CreateHandler() =>
        new(_releaseRepo, _workItemRepo, _historyService, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ValidCommand_UnassignsItemFromRelease()
    {
        var release = ReleaseBuilder.New().Build();
        var workItem = WorkItemBuilder.New().WithRelease(release.Id).Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);
        _workItemRepo.UpdateAsync(workItem, Arg.Any<CancellationToken>()).Returns(workItem);

        var result = await CreateHandler().Handle(
            new UnassignItemFromReleaseCommand(release.Id, workItem.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        workItem.ReleaseId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReleaseNotFound_ReturnsFailure()
    {
        _releaseRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Release?)null);

        var result = await CreateHandler().Handle(
            new UnassignItemFromReleaseCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Release not found");
    }

    [Fact]
    public async Task Handle_WorkItemNotInRelease_ReturnsFailure()
    {
        var release = ReleaseBuilder.New().Build();
        var workItem = WorkItemBuilder.New().Build(); // No release assigned
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var result = await CreateHandler().Handle(
            new UnassignItemFromReleaseCommand(release.Id, workItem.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not assigned");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var release = ReleaseBuilder.New().Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        _permissions.HasPermissionAsync(ActorId, release.ProjectId, Permission.Release_Edit, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(
            new UnassignItemFromReleaseCommand(release.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
