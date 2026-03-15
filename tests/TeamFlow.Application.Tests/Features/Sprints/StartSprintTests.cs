using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.StartSprint;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

public sealed class StartSprintTests
{
    private readonly ISprintRepository _sprintRepo = Substitute.For<ISprintRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public StartSprintTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _sprintRepo.UpdateAsync(Arg.Any<Sprint>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Sprint>());
        _sprintRepo.GetActiveSprintForProjectAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Sprint?)null);
    }

    private StartSprintHandler CreateHandler() =>
        new(_sprintRepo, _currentUser, _permissions, _publisher);

    [Fact]
    public async Task Handle_PlanningSprintWithItemsAndDates_StartSucceeds()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New()
            .WithProject(projectId)
            .WithDates(new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 30))
            .WithStatus(SprintStatus.Planning)
            .Build();

        var workItem = WorkItemBuilder.New().WithProject(projectId).WithSprint(sprint.Id).Build();
        sprint.WorkItems = [workItem];

        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var cmd = new StartSprintCommand(sprint.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(SprintStatus.Active);
    }

    [Fact]
    public async Task Handle_SprintWithNoItems_ReturnsError()
    {
        var sprint = SprintBuilder.New()
            .WithDates(new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 30))
            .WithStatus(SprintStatus.Planning)
            .Build();
        sprint.WorkItems = [];

        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var cmd = new StartSprintCommand(sprint.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least one item");
    }

    [Fact]
    public async Task Handle_AnotherActiveSprint_ReturnsConflict()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New()
            .WithProject(projectId)
            .WithDates(new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 30))
            .WithStatus(SprintStatus.Planning)
            .Build();

        var workItem = WorkItemBuilder.New().WithProject(projectId).Build();
        sprint.WorkItems = [workItem];

        var activeSprint = SprintBuilder.New().WithProject(projectId).Active().Build();

        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _sprintRepo.GetActiveSprintForProjectAsync(projectId, Arg.Any<CancellationToken>()).Returns(activeSprint);

        var cmd = new StartSprintCommand(sprint.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already active");
    }

    [Fact]
    public async Task Handle_StartSprint_PublishesSprintStartedDomainEvent()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New()
            .WithProject(projectId)
            .WithDates(new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 30))
            .WithStatus(SprintStatus.Planning)
            .Build();

        var workItem = WorkItemBuilder.New().WithProject(projectId).Build();
        sprint.WorkItems = [workItem];

        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var cmd = new StartSprintCommand(sprint.Id);
        await CreateHandler().Handle(cmd, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<SprintStartedDomainEvent>(e => e.SprintId == sprint.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SprintWithoutDates_ReturnsError()
    {
        var sprint = SprintBuilder.New().WithStatus(SprintStatus.Planning).Build();
        var workItem = WorkItemBuilder.New().Build();
        sprint.WorkItems = [workItem];

        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var cmd = new StartSprintCommand(sprint.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("start and end dates");
    }

    [Fact]
    public async Task Handle_NonTeamManager_ReturnsAccessDenied()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New().WithProject(projectId).WithStatus(SprintStatus.Planning).Build();
        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _permissions.HasPermissionAsync(ActorId, projectId, Permission.Sprint_Start, Arg.Any<CancellationToken>())
            .Returns(false);

        var cmd = new StartSprintCommand(sprint.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
