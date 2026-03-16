using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.Dtos;
using TeamFlow.Application.Features.Dashboard.GetCycleTime;

namespace TeamFlow.Application.Tests.Features.Dashboard;

public sealed class GetCycleTimeTests
{
    private readonly IDashboardRepository _dashRepo = Substitute.For<IDashboardRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public GetCycleTimeTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private GetCycleTimeHandler CreateHandler() => new(_dashRepo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ValidProject_ReturnsCycleTimeMetrics()
    {
        var byType = new List<CycleTimeByTypeDto>
        {
            new("Bug", 2.5, 2.0, 4.0, 15),
            new("Story", 5.1, 4.8, 8.2, 30),
        };
        _dashRepo.GetCycleTimeDataAsync(ProjectId, null, null, Arg.Any<CancellationToken>())
            .Returns(new CycleTimeDto(byType));

        var result = await CreateHandler().Handle(new GetCycleTimeQuery(ProjectId, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByType.Should().HaveCount(2);
        result.Value.ByType[0].AvgDays.Should().BeApproximately(2.5, 0.001);
        result.Value.ByType[0].MedianDays.Should().BeApproximately(2.0, 0.001);
        result.Value.ByType[0].P90Days.Should().BeApproximately(4.0, 0.001);
    }

    [Fact]
    public async Task Handle_WithDateRange_PassesDatesToRepository()
    {
        var from = new DateOnly(2026, 2, 1);
        var to = new DateOnly(2026, 2, 28);
        _dashRepo.GetCycleTimeDataAsync(ProjectId, from, to, Arg.Any<CancellationToken>())
            .Returns(new CycleTimeDto([]));

        var result = await CreateHandler().Handle(new GetCycleTimeQuery(ProjectId, from, to), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _dashRepo.Received(1).GetCycleTimeDataAsync(ProjectId, from, to, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoData_ReturnsEmptyByType()
    {
        _dashRepo.GetCycleTimeDataAsync(ProjectId, null, null, Arg.Any<CancellationToken>())
            .Returns(new CycleTimeDto([]));

        var result = await CreateHandler().Handle(new GetCycleTimeQuery(ProjectId, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ByType.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(new GetCycleTimeQuery(ProjectId, null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
