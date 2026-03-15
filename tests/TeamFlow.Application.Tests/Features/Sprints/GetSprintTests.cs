using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.GetSprint;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

public sealed class GetSprintTests
{
    private readonly ISprintRepository _sprintRepo = Substitute.For<ISprintRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public GetSprintTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private GetSprintHandler CreateHandler() =>
        new(_sprintRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ExistingSprint_ReturnsDetailDto()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New()
            .WithProject(projectId)
            .WithName("Sprint 1")
            .WithGoal("Deliver feature X")
            .Build();

        var workItem = WorkItemBuilder.New()
            .WithProject(projectId)
            .WithSprint(sprint.Id)
            .WithEstimation(5)
            .WithStatus(WorkItemStatus.Done)
            .Build();

        sprint.WorkItems = [workItem];

        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);

        var query = new GetSprintQuery(sprint.Id);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Sprint 1");
        result.Value.Goal.Should().Be("Deliver feature X");
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalPoints.Should().Be(5);
        result.Value.CompletedPoints.Should().Be(5);
    }

    [Fact]
    public async Task Handle_NonExistentSprint_ReturnsNotFoundError()
    {
        _sprintRepo.GetByIdWithItemsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sprint?)null);

        var query = new GetSprintQuery(Guid.NewGuid());
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sprint not found");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var sprint = SprintBuilder.New().Build();
        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _permissions.HasPermissionAsync(ActorId, sprint.ProjectId, Permission.Project_View, Arg.Any<CancellationToken>())
            .Returns(false);

        var query = new GetSprintQuery(sprint.Id);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
