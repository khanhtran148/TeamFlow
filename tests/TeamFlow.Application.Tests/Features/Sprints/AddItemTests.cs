using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.AddItem;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

public sealed class AddItemTests
{
    private readonly ISprintRepository _sprintRepo = Substitute.For<ISprintRepository>();
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public AddItemTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _workItemRepo.UpdateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<WorkItem>());
    }

    private AddItemToSprintHandler CreateHandler() =>
        new(_sprintRepo, _workItemRepo, _historyService, _currentUser, _permissions, _publisher);

    [Fact]
    public async Task Handle_AddItemToPlanningSprintSucceeds()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New().WithProject(projectId).WithStatus(SprintStatus.Planning).Build();
        var workItem = WorkItemBuilder.New().WithProject(projectId).Build();

        _sprintRepo.GetByIdAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var cmd = new AddItemToSprintCommand(sprint.Id, workItem.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        workItem.SprintId.Should().Be(sprint.Id);
    }

    [Fact]
    public async Task Handle_AddItemToActiveSprintWithoutElevatedPermission_ReturnsForbidden()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New().WithProject(projectId).Active().Build();

        _sprintRepo.GetByIdAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _permissions.HasPermissionAsync(ActorId, projectId, Permission.Sprint_Start, Arg.Any<CancellationToken>())
            .Returns(false);

        var workItem = WorkItemBuilder.New().WithProject(projectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var cmd = new AddItemToSprintCommand(sprint.Id, workItem.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_ItemAlreadyInAnotherSprint_ReturnsConflict()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New().WithProject(projectId).WithStatus(SprintStatus.Planning).Build();
        var otherSprintId = Guid.NewGuid();
        var workItem = WorkItemBuilder.New().WithProject(projectId).WithSprint(otherSprintId).Build();

        _sprintRepo.GetByIdAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var cmd = new AddItemToSprintCommand(sprint.Id, workItem.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already belongs to another sprint");
    }

    [Fact]
    public async Task Handle_AddItemToCompletedSprint_ReturnsError()
    {
        var sprint = SprintBuilder.New().Completed().Build();
        _sprintRepo.GetByIdAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var cmd = new AddItemToSprintCommand(sprint.Id, Guid.NewGuid());
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot add items");
    }

    [Fact]
    public async Task Handle_AddItem_PublishesSprintItemAddedDomainEvent()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New().WithProject(projectId).WithStatus(SprintStatus.Planning).Build();
        var workItem = WorkItemBuilder.New().WithProject(projectId).Build();

        _sprintRepo.GetByIdAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var cmd = new AddItemToSprintCommand(sprint.Id, workItem.Id);
        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<SprintItemAddedDomainEvent>(e =>
                e.SprintId == sprint.Id && e.WorkItemId == workItem.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AddItem_RecordsHistory()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New().WithProject(projectId).WithStatus(SprintStatus.Planning).Build();
        var workItem = WorkItemBuilder.New().WithProject(projectId).Build();

        _sprintRepo.GetByIdAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var cmd = new AddItemToSprintCommand(sprint.Id, workItem.Id);
        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _historyService.Received(1).RecordAsync(
            Arg.Is<WorkItemHistoryEntry>(h =>
                h.WorkItemId == workItem.Id && h.ActionType == "SprintAssigned"),
            Arg.Any<CancellationToken>());
    }
}
