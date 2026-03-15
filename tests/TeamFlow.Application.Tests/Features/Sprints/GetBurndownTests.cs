using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.GetBurndown;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

public sealed class GetBurndownTests
{
    private readonly ISprintRepository _sprintRepo = Substitute.For<ISprintRepository>();
    private readonly IBurndownDataPointRepository _burndownRepo = Substitute.For<IBurndownDataPointRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public GetBurndownTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private GetBurndownHandler CreateHandler() =>
        new(_sprintRepo, _burndownRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_SprintWithDataPoints_ReturnsBurndownData()
    {
        var projectId = Guid.NewGuid();
        var sprint = SprintBuilder.New()
            .WithProject(projectId)
            .WithDates(new DateOnly(2026, 3, 16), new DateOnly(2026, 3, 20))
            .Active()
            .Build();

        var workItem = WorkItemBuilder.New()
            .WithProject(projectId)
            .WithEstimation(10)
            .Build();

        sprint.WorkItems = [workItem];

        var dataPoint = BurndownDataPointBuilder.New()
            .WithSprint(sprint.Id)
            .WithDate(new DateOnly(2026, 3, 16))
            .WithRemainingPoints(8)
            .WithCompletedPoints(2)
            .Build();

        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _burndownRepo.GetBySprintAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns([dataPoint]);

        var query = new GetBurndownQuery(sprint.Id);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SprintId.Should().Be(sprint.Id);
        result.Value.ActualLine.Should().HaveCount(1);
        result.Value.IdealLine.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_EmptySprint_ReturnsEmptyArrays()
    {
        var sprint = SprintBuilder.New().Build();
        sprint.WorkItems = [];

        _sprintRepo.GetByIdWithItemsAsync(sprint.Id, Arg.Any<CancellationToken>()).Returns(sprint);
        _burndownRepo.GetBySprintAsync(sprint.Id, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<BurndownDataPoint>());

        var query = new GetBurndownQuery(sprint.Id);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ActualLine.Should().BeEmpty();
        result.Value.IdealLine.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_SprintNotFound_ReturnsError()
    {
        _sprintRepo.GetByIdWithItemsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Sprint?)null);

        var query = new GetBurndownQuery(Guid.NewGuid());
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Sprint not found");
    }
}
