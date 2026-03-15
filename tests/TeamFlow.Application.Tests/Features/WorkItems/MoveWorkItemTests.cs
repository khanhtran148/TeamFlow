using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems.MoveWorkItem;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

public sealed class MoveWorkItemTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    public MoveWorkItemTests()
    {
        _currentUser.Id.Returns(Guid.NewGuid());
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private MoveWorkItemHandler CreateHandler() =>
        new(_workItemRepo, _historyService, _currentUser, _permissions);

    [Fact]
    public async Task Handle_StoryMovedToNewEpic_Succeeds()
    {
        var projectId = Guid.NewGuid();
        var story = WorkItemBuilder.New().WithProject(projectId).WithType(WorkItemType.UserStory).Build();
        var newEpic = WorkItemBuilder.New().WithProject(projectId).AsEpic().Build();

        _workItemRepo.GetByIdAsync(story.Id, Arg.Any<CancellationToken>()).Returns(story);
        _workItemRepo.GetByIdAsync(newEpic.Id, Arg.Any<CancellationToken>()).Returns(newEpic);
        _workItemRepo.UpdateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<WorkItem>());

        var cmd = new MoveWorkItemCommand(story.Id, newEpic.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        story.ParentId.Should().Be(newEpic.Id);
    }

    [Fact]
    public async Task Handle_InvalidReparent_ReturnsError()
    {
        var projectId = Guid.NewGuid();
        var story = WorkItemBuilder.New().WithProject(projectId).WithType(WorkItemType.UserStory).Build();
        var anotherStory = WorkItemBuilder.New().WithProject(projectId).WithType(WorkItemType.UserStory).Build();

        _workItemRepo.GetByIdAsync(story.Id, Arg.Any<CancellationToken>()).Returns(story);
        _workItemRepo.GetByIdAsync(anotherStory.Id, Arg.Any<CancellationToken>()).Returns(anotherStory);

        // Story cannot be moved under another Story
        var cmd = new MoveWorkItemCommand(story.Id, anotherStory.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonExistentItem_ReturnsNotFound()
    {
        var itemId = Guid.NewGuid();
        _workItemRepo.GetByIdAsync(itemId, Arg.Any<CancellationToken>()).Returns((WorkItem?)null);

        var cmd = new MoveWorkItemCommand(itemId, Guid.NewGuid());
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
