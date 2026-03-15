using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.CompleteSprint;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

public sealed class CompleteSprintTests
{
    private readonly ISprintRepository _sprintRepo = Substitute.For<ISprintRepository>();
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public CompleteSprintTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _sprintRepo.UpdateAsync(Arg.Any<Sprint>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Sprint>());
        _workItemRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var item = WorkItemBuilder.New().Build();
                return item;
            });
        _workItemRepo.UpdateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<WorkItem>());
    }

    private CompleteSprintHandler CreateHandler() =>
        new(_sprintRepo, _workItemRepo, _currentUser, _permissions, _publisher);

    [Fact]
    public async Task Handle_ActiveSprint_CompletesSuccessfully()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New()
            .WithProject(projectId)
            .WithDates(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 15))
            .Active()
            .Build();

        var doneItem = WorkItemBuilder.New()
            .WithProject(projectId)
            .WithSprint(sprint.Id)
            .WithStatus(WorkItemStatus.Done)
            .WithEstimation(5)
            .Build();

        sprint.WorkItems = [doneItem];
        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var cmd = new CompleteSprintCommand(sprint.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(SprintStatus.Completed);
    }

    [Fact]
    public async Task Handle_PlanningSprintComplete_ReturnsError()
    {
        var sprint = SprintBuilder.New().WithStatus(SprintStatus.Planning).Build();
        sprint.WorkItems = [];
        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var cmd = new CompleteSprintCommand(sprint.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Only an Active sprint");
    }

    [Fact]
    public async Task Handle_CompleteSprint_CarriesOverIncompleteItems()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New()
            .WithProject(projectId)
            .Active()
            .Build();

        var incompleteItem = WorkItemBuilder.New()
            .WithProject(projectId)
            .WithSprint(sprint.Id)
            .WithStatus(WorkItemStatus.InProgress)
            .Build();

        var doneItem = WorkItemBuilder.New()
            .WithProject(projectId)
            .WithSprint(sprint.Id)
            .WithStatus(WorkItemStatus.Done)
            .Build();

        sprint.WorkItems = [incompleteItem, doneItem];

        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _workItemRepo.GetByIdAsync(incompleteItem.Id, Arg.Any<CancellationToken>()).Returns(incompleteItem);

        var cmd = new CompleteSprintCommand(sprint.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // The incomplete item should have been fetched and updated
        await _workItemRepo.Received(1).GetByIdAsync(incompleteItem.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CompleteSprint_PublishesSprintCompletedDomainEvent()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New()
            .WithProject(projectId)
            .Active()
            .Build();

        var item = WorkItemBuilder.New()
            .WithProject(projectId)
            .WithSprint(sprint.Id)
            .WithStatus(WorkItemStatus.Done)
            .WithEstimation(8)
            .Build();

        sprint.WorkItems = [item];
        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var cmd = new CompleteSprintCommand(sprint.Id);
        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<SprintCompletedDomainEvent>(e =>
                e.SprintId == sprint.Id &&
                e.PlannedPoints == 8 &&
                e.CompletedPoints == 8),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New().WithProject(projectId).Active().Build();
        sprint.WorkItems = [];
        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _permissions.HasPermissionAsync(ActorId, projectId, Permission.Sprint_Complete, Arg.Any<CancellationToken>())
            .Returns(false);

        var cmd = new CompleteSprintCommand(sprint.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
