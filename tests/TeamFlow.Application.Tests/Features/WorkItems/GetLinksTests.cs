using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems.GetLinks;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

public sealed class GetLinksTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IWorkItemLinkRepository _linkRepo = Substitute.For<IWorkItemLinkRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    public GetLinksTests()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private GetWorkItemLinksHandler CreateHandler() =>
        new(_workItemRepo, _linkRepo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ItemWithLinks_ReturnsGroupedLinks()
    {
        var itemA = WorkItemBuilder.New().WithType(WorkItemType.Task).Build();
        var itemB = WorkItemBuilder.New().WithType(WorkItemType.Bug).Build();
        var link = new WorkItemLink
        {
            SourceId = itemA.Id,
            TargetId = itemB.Id,
            LinkType = LinkType.Blocks,
            Scope = LinkScope.SameProject,
            CreatedById = Guid.NewGuid(),
            Source = itemA,
            Target = itemB
        };

        _workItemRepo.GetByIdAsync(itemA.Id, Arg.Any<CancellationToken>()).Returns(itemA);
        _linkRepo.GetLinksForItemAsync(itemA.Id, Arg.Any<CancellationToken>()).Returns(new[] { link });

        var result = await CreateHandler().Handle(new GetWorkItemLinksQuery(itemA.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Groups.Should().HaveCount(1);
        result.Value.Groups.First().LinkType.Should().Be(LinkType.Blocks);
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        var itemId = Guid.NewGuid();
        _workItemRepo.GetByIdAsync(itemId, Arg.Any<CancellationToken>()).Returns((WorkItem?)null);

        var result = await CreateHandler().Handle(new GetWorkItemLinksQuery(itemId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
