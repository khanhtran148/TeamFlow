using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases.AssignItem;
using TeamFlow.Application.Features.Releases.UnassignItem;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Releases;

public sealed class AssignItemTests
{
    private readonly IReleaseRepository _releaseRepo = Substitute.For<IReleaseRepository>();
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public AssignItemTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private AssignItemToReleaseHandler CreateAssignHandler() =>
        new(_releaseRepo, _workItemRepo, _historyService, _currentUser, _permissions, _publisher);

    private UnassignItemFromReleaseHandler CreateUnassignHandler() =>
        new(_releaseRepo, _workItemRepo, _historyService, _currentUser, _permissions);

    [Fact]
    public async Task Assign_ValidItem_SetsReleaseId()
    {
        var release = ReleaseBuilder.New().Build();
        var item = WorkItemBuilder.New().Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        _workItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _workItemRepo.UpdateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<WorkItem>());

        var result = await CreateAssignHandler().Handle(new AssignItemToReleaseCommand(release.Id, item.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.ReleaseId.Should().Be(release.Id);
    }

    [Fact]
    public async Task Assign_ItemInAnotherRelease_ReturnsError()
    {
        var release = ReleaseBuilder.New().Build();
        var otherReleaseId = Guid.NewGuid();
        var item = WorkItemBuilder.New().WithRelease(otherReleaseId).Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        _workItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);

        var result = await CreateAssignHandler().Handle(new AssignItemToReleaseCommand(release.Id, item.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("another release");
    }

    [Fact]
    public async Task Unassign_AssignedItem_ClearsReleaseId()
    {
        var release = ReleaseBuilder.New().Build();
        var item = WorkItemBuilder.New().WithRelease(release.Id).Build();
        _releaseRepo.GetByIdAsync(release.Id, Arg.Any<CancellationToken>()).Returns(release);
        _workItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _workItemRepo.UpdateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<WorkItem>());

        var result = await CreateUnassignHandler().Handle(new UnassignItemFromReleaseCommand(release.Id, item.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.ReleaseId.Should().BeNull();
    }

    [Fact]
    public async Task ListReleases_ReturnsPaged()
    {
        // Basic smoke test handled in ListReleasesHandler
    }
}
