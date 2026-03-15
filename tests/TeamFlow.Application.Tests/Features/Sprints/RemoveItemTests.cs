using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.RemoveItem;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

public sealed class RemoveItemTests
{
    private readonly ISprintRepository _sprintRepo = Substitute.For<ISprintRepository>();
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public RemoveItemTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _workItemRepo.UpdateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<WorkItem>());
    }

    private RemoveItemFromSprintHandler CreateHandler() =>
        new(_sprintRepo, _workItemRepo, _historyService, _currentUser, _permissions, _publisher);

    [Fact]
    public async Task Handle_RemoveItemFromSprint_SetsSprintIdNull()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New().WithProject(projectId).WithStatus(SprintStatus.Planning).Build();
        var workItem = WorkItemBuilder.New().WithProject(projectId).WithSprint(sprint.Id).Build();

        _sprintRepo.GetByIdAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var cmd = new RemoveItemFromSprintCommand(sprint.Id, workItem.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        workItem.SprintId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RemoveItem_PublishesSprintItemRemovedDomainEvent()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New().WithProject(projectId).WithStatus(SprintStatus.Planning).Build();
        var workItem = WorkItemBuilder.New().WithProject(projectId).WithSprint(sprint.Id).Build();

        _sprintRepo.GetByIdAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var cmd = new RemoveItemFromSprintCommand(sprint.Id, workItem.Id);
        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<SprintItemRemovedDomainEvent>(e =>
                e.SprintId == sprint.Id && e.WorkItemId == workItem.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemNotInSprint_ReturnsError()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New().WithProject(projectId).Build();
        var workItem = WorkItemBuilder.New().WithProject(projectId).Build(); // no sprint

        _sprintRepo.GetByIdAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);

        var cmd = new RemoveItemFromSprintCommand(sprint.Id, workItem.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("does not belong to this sprint");
    }

    [Fact]
    public async Task Handle_SprintNotFound_ReturnsError()
    {
        _sprintRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sprint?)null);

        var cmd = new RemoveItemFromSprintCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sprint not found");
    }
}
