using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems.CheckBlockers;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

public sealed class CheckBlockersTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IWorkItemLinkRepository _linkRepo = Substitute.For<IWorkItemLinkRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public CheckBlockersTests()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private CheckBlockersHandler CreateHandler() =>
        new(_workItemRepo, _linkRepo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_NoBlockers_ReturnsEmptyList()
    {
        var item = WorkItemBuilder.New().Build();
        _workItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _linkRepo.GetBlockersForItemAsync(item.Id, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<WorkItemLink>());

        var result = await CreateHandler().Handle(new CheckBlockersQuery(item.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasUnresolvedBlockers.Should().BeFalse();
        result.Value.Blockers.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DoneBlocker_NotReturned()
    {
        var item = WorkItemBuilder.New().Build();
        var blocker = WorkItemBuilder.New().WithStatus(WorkItemStatus.Done).Build();
        var link = new WorkItemLink
        {
            SourceId = blocker.Id,
            TargetId = item.Id,
            LinkType = LinkType.Blocks,
            Scope = LinkScope.SameProject,
            CreatedById = Guid.NewGuid(),
            Source = blocker
        };
        _workItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _linkRepo.GetBlockersForItemAsync(item.Id, Arg.Any<CancellationToken>()).Returns(new[] { link });

        var result = await CreateHandler().Handle(new CheckBlockersQuery(item.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasUnresolvedBlockers.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ActiveBlocker_ReturnedInList()
    {
        var item = WorkItemBuilder.New().Build();
        var blocker = WorkItemBuilder.New().WithStatus(WorkItemStatus.InProgress).WithTitle("Blocker Task").Build();
        var link = new WorkItemLink
        {
            SourceId = blocker.Id,
            TargetId = item.Id,
            LinkType = LinkType.Blocks,
            Scope = LinkScope.SameProject,
            CreatedById = Guid.NewGuid(),
            Source = blocker
        };
        _workItemRepo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _linkRepo.GetBlockersForItemAsync(item.Id, Arg.Any<CancellationToken>()).Returns(new[] { link });

        var result = await CreateHandler().Handle(new CheckBlockersQuery(item.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasUnresolvedBlockers.Should().BeTrue();
        result.Value.Blockers.Should().HaveCount(1);
        result.Value.Blockers.First().Title.Should().Be("Blocker Task");
    }
}
