using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems.RemoveLink;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

public sealed class RemoveLinkTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IWorkItemLinkRepository _linkRepo = Substitute.For<IWorkItemLinkRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public RemoveLinkTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private RemoveWorkItemLinkHandler CreateHandler() =>
        new(_workItemRepo, _linkRepo, _historyService, _currentUser, _permissions, _publisher);

    [Fact]
    public async Task Handle_ExistingLink_RemovesBothDirections()
    {
        var sourceId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var link = WorkItemLinkBuilder.New()
            .WithSource(sourceId).WithTarget(targetId).WithLinkType(LinkType.RelatesTo).Build();
        var sourceItem = WorkItemBuilder.New().Build();
        var targetItem = WorkItemBuilder.New().Build();

        _linkRepo.GetByIdAsync(link.Id, Arg.Any<CancellationToken>()).Returns(link);
        _workItemRepo.GetByIdAsync(sourceId, Arg.Any<CancellationToken>()).Returns(sourceItem);
        _workItemRepo.GetByIdAsync(targetId, Arg.Any<CancellationToken>()).Returns(targetItem);

        var result = await CreateHandler().Handle(new RemoveWorkItemLinkCommand(link.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _linkRepo.Received(1).DeletePairAsync(sourceId, targetId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentLink_ReturnsNotFound()
    {
        var linkId = Guid.NewGuid();
        _linkRepo.GetByIdAsync(linkId, Arg.Any<CancellationToken>()).Returns((WorkItemLink?)null);

        var result = await CreateHandler().Handle(new RemoveWorkItemLinkCommand(linkId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
