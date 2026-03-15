using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.DeleteSprint;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

public sealed class DeleteSprintTests
{
    private readonly ISprintRepository _sprintRepo = Substitute.For<ISprintRepository>();
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public DeleteSprintTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _workItemRepo.UpdateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<WorkItem>());
    }

    private DeleteSprintHandler CreateHandler() =>
        new(_sprintRepo, _workItemRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_PlanningSprintDelete_Succeeds()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New()
            .WithProject(projectId)
            .WithStatus(SprintStatus.Planning)
            .Build();

        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var cmd = new DeleteSprintCommand(sprint.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _sprintRepo.Received(1).DeleteAsync(sprint, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeletePlanningSprintWithItems_UnlinksItems()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New()
            .WithProject(projectId)
            .WithStatus(SprintStatus.Planning)
            .Build();

        var item = WorkItemBuilder.New().WithProject(projectId).WithSprint(sprint.Id).Build();
        sprint.WorkItems = [item];

        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var cmd = new DeleteSprintCommand(sprint.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.SprintId.Should().BeNull();
        await _workItemRepo.Received(1).UpdateAsync(item, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ActiveSprint_ReturnsError()
    {
        var sprint = SprintBuilder.New().Active().Build();
        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var cmd = new DeleteSprintCommand(sprint.Id);
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not in Planning status");
    }

    [Fact]
    public async Task Handle_SprintNotFound_ReturnsError()
    {
        _sprintRepo.GetByIdWithItemsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sprint?)null);

        var cmd = new DeleteSprintCommand(Guid.NewGuid());
        var result = await CreateHandler().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sprint not found");
    }
}
