using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems.AddLink;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

public sealed class AddLinkTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IWorkItemLinkRepository _linkRepo = Substitute.For<IWorkItemLinkRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid ActorId = Guid.NewGuid();

    public AddLinkTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private AddWorkItemLinkHandler CreateHandler() =>
        new(_workItemRepo, _linkRepo, _historyService, _currentUser, _permissions, _publisher);

    [Fact]
    public async Task Handle_ValidLink_CreatesBidirectionalPair()
    {
        var itemA = WorkItemBuilder.New().WithProject(ProjectId).Build();
        var itemB = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(itemA.Id, Arg.Any<CancellationToken>()).Returns(itemA);
        _workItemRepo.GetByIdAsync(itemB.Id, Arg.Any<CancellationToken>()).Returns(itemB);
        _linkRepo.ExistsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<LinkType>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _linkRepo.GetReachableTargetsAsync(Arg.Any<Guid>(), Arg.Any<LinkType>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<Guid>());

        var cmd = new AddWorkItemLinkCommand(itemA.Id, itemB.Id, LinkType.RelatesTo);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _linkRepo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<WorkItemLink>>(links => links.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateLink_ReturnsConflict()
    {
        var itemA = WorkItemBuilder.New().WithProject(ProjectId).Build();
        var itemB = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(itemA.Id, Arg.Any<CancellationToken>()).Returns(itemA);
        _workItemRepo.GetByIdAsync(itemB.Id, Arg.Any<CancellationToken>()).Returns(itemB);
        _linkRepo.ExistsAsync(itemA.Id, itemB.Id, LinkType.RelatesTo, Arg.Any<CancellationToken>())
            .Returns(true);

        var cmd = new AddWorkItemLinkCommand(itemA.Id, itemB.Id, LinkType.RelatesTo);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_CircularBlock_ReturnsConflict()
    {
        var itemA = WorkItemBuilder.New().WithProject(ProjectId).Build();
        var itemB = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(itemA.Id, Arg.Any<CancellationToken>()).Returns(itemA);
        _workItemRepo.GetByIdAsync(itemB.Id, Arg.Any<CancellationToken>()).Returns(itemB);
        _linkRepo.ExistsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<LinkType>(), Arg.Any<CancellationToken>())
            .Returns(false);
        // B already reaches A via Blocks chain -> adding A Blocks B would be circular
        _linkRepo.GetReachableTargetsAsync(itemB.Id, LinkType.Blocks, Arg.Any<CancellationToken>())
            .Returns(new[] { itemA.Id });

        var cmd = new AddWorkItemLinkCommand(itemA.Id, itemB.Id, LinkType.Blocks);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Circular");
    }

    [Fact]
    public async Task Handle_CrossProjectLink_SetsScopeCorrectly()
    {
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();
        var itemA = WorkItemBuilder.New().WithProject(projectA).Build();
        var itemB = WorkItemBuilder.New().WithProject(projectB).Build();
        _workItemRepo.GetByIdAsync(itemA.Id, Arg.Any<CancellationToken>()).Returns(itemA);
        _workItemRepo.GetByIdAsync(itemB.Id, Arg.Any<CancellationToken>()).Returns(itemB);
        _linkRepo.ExistsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<LinkType>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _linkRepo.GetReachableTargetsAsync(Arg.Any<Guid>(), Arg.Any<LinkType>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<Guid>());

        IEnumerable<WorkItemLink>? capturedLinks = null;
        await _linkRepo.AddRangeAsync(
            Arg.Do<IEnumerable<WorkItemLink>>(links => capturedLinks = links.ToList()),
            Arg.Any<CancellationToken>());

        var cmd = new AddWorkItemLinkCommand(itemA.Id, itemB.Id, LinkType.RelatesTo);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedLinks.Should().NotBeNull();
        capturedLinks!.All(l => l.Scope == LinkScope.CrossProject).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MissingSourceItem_ReturnsNotFound()
    {
        _workItemRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((WorkItem?)null);

        var cmd = new AddWorkItemLinkCommand(Guid.NewGuid(), Guid.NewGuid(), LinkType.RelatesTo);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
