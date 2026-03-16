using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.Dtos;
using TeamFlow.Application.Features.Dashboard.GetCumulativeFlow;

namespace TeamFlow.Application.Tests.Features.Dashboard;

public sealed class GetCumulativeFlowTests
{
    private readonly IDashboardRepository _dashRepo = Substitute.For<IDashboardRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public GetCumulativeFlowTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private GetCumulativeFlowHandler CreateHandler() => new(_dashRepo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ValidDateRange_ReturnsFlowData()
    {
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 1, 31);
        var dataPoints = new List<CumulativeFlowPointDto>
        {
            new(new DateOnly(2026, 1, 15), 5, 3, 2, 10),
            new(new DateOnly(2026, 1, 16), 4, 4, 2, 12),
        };
        _dashRepo.GetCumulativeFlowDataAsync(ProjectId, from, to, Arg.Any<CancellationToken>())
            .Returns(new CumulativeFlowDto(dataPoints));

        var result = await CreateHandler().Handle(new GetCumulativeFlowQuery(ProjectId, from, to), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DataPoints.Should().HaveCount(2);
        result.Value.DataPoints[0].Done.Should().Be(10);
    }

    [Fact]
    public async Task Handle_EmptyProject_ReturnsEmptyDataPoints()
    {
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);
        _dashRepo.GetCumulativeFlowDataAsync(ProjectId, from, to, Arg.Any<CancellationToken>())
            .Returns(new CumulativeFlowDto([]));

        var result = await CreateHandler().Handle(new GetCumulativeFlowQuery(ProjectId, from, to), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DataPoints.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var query = new GetCumulativeFlowQuery(ProjectId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
