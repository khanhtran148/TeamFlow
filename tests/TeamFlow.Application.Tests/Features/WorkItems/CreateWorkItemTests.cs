using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.WorkItems.CreateWorkItem;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.WorkItems;

public sealed class CreateWorkItemTests
{
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public CreateWorkItemTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _workItemRepo.AddAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<WorkItem>());
    }

    private CreateWorkItemHandler CreateHandler() =>
        new(_workItemRepo, _projectRepo, _historyService, _currentUser, _permissions, _publisher);

    [Fact]
    public async Task Handle_Epic_CreatesWithNoParent()
    {
        var projectId = Guid.NewGuid();
        _projectRepo.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);

        var cmd = new CreateWorkItemCommand(projectId, null, WorkItemType.Epic, "My Epic", null, Priority.High, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Type.Should().Be(WorkItemType.Epic);
        result.Value.ParentId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Epic_WithParent_ReturnsValidationError()
    {
        var projectId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        _projectRepo.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);

        var cmd = new CreateWorkItemCommand(projectId, parentId, WorkItemType.Epic, "My Epic", null, Priority.High, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Epic cannot have a parent");
    }

    [Fact]
    public async Task Handle_Story_WithEpicParent_Succeeds()
    {
        var projectId = Guid.NewGuid();
        var epic = WorkItemBuilder.New().WithProject(projectId).AsEpic().Build();
        _projectRepo.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);
        _workItemRepo.GetByIdAsync(epic.Id, Arg.Any<CancellationToken>()).Returns(epic);

        var cmd = new CreateWorkItemCommand(projectId, epic.Id, WorkItemType.UserStory, "My Story", null, Priority.Medium, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ParentId.Should().Be(epic.Id);
    }

    [Fact]
    public async Task Handle_Story_WithStoryParent_ReturnsValidationError()
    {
        var projectId = Guid.NewGuid();
        var storyParent = WorkItemBuilder.New().WithProject(projectId).WithType(WorkItemType.UserStory).Build();
        _projectRepo.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);
        _workItemRepo.GetByIdAsync(storyParent.Id, Arg.Any<CancellationToken>()).Returns(storyParent);

        var cmd = new CreateWorkItemCommand(projectId, storyParent.Id, WorkItemType.UserStory, "My Story", null, Priority.Medium, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("UserStory parent must be an Epic");
    }

    [Theory]
    [InlineData(WorkItemType.Task)]
    [InlineData(WorkItemType.Bug)]
    [InlineData(WorkItemType.Spike)]
    public async Task Handle_TaskBugSpike_WithStoryParent_Succeeds(WorkItemType type)
    {
        var projectId = Guid.NewGuid();
        var story = WorkItemBuilder.New().WithProject(projectId).WithType(WorkItemType.UserStory).Build();
        _projectRepo.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);
        _workItemRepo.GetByIdAsync(story.Id, Arg.Any<CancellationToken>()).Returns(story);

        var cmd = new CreateWorkItemCommand(projectId, story.Id, type, "My Item", null, Priority.Low, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(WorkItemType.Task)]
    [InlineData(WorkItemType.Bug)]
    [InlineData(WorkItemType.Spike)]
    public async Task Handle_TaskBugSpike_WithEpicParent_ReturnsError(WorkItemType type)
    {
        var projectId = Guid.NewGuid();
        var epic = WorkItemBuilder.New().WithProject(projectId).AsEpic().Build();
        _projectRepo.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);
        _workItemRepo.GetByIdAsync(epic.Id, Arg.Any<CancellationToken>()).Returns(epic);

        var cmd = new CreateWorkItemCommand(projectId, epic.Id, type, "My Item", null, Priority.Low, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("must have a UserStory parent");
    }

    [Fact]
    public async Task Handle_MissingProject_ReturnsNotFound()
    {
        var projectId = Guid.NewGuid();
        _projectRepo.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(false);

        var cmd = new CreateWorkItemCommand(projectId, null, WorkItemType.Epic, "Epic", null, Priority.High, null);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Project not found");
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesDomainEvent()
    {
        var projectId = Guid.NewGuid();
        _projectRepo.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);

        var cmd = new CreateWorkItemCommand(projectId, null, WorkItemType.Epic, "My Epic", null, Priority.High, null);
        await CreateHandler().Handle(cmd, CancellationToken.None);

        // Verify that publisher.Publish was called (WorkItemCreatedDomainEvent implements INotification)
        await _publisher.Received(1).Publish(
            Arg.Any<WorkItemCreatedDomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task Validate_EmptyTitle_ReturnsValidationError(string? title)
    {
        var validator = new CreateWorkItemValidator();
        var cmd = new CreateWorkItemCommand(Guid.NewGuid(), null, WorkItemType.Epic, title!, null, null, null);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }
}
