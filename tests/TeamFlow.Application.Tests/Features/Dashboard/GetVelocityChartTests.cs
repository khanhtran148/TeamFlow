using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.Dtos;
using TeamFlow.Application.Features.Dashboard.GetVelocityChart;

namespace TeamFlow.Application.Tests.Features.Dashboard;

public sealed class GetVelocityChartTests
{
    private readonly IDashboardRepository _dashRepo = Substitute.For<IDashboardRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public GetVelocityChartTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private GetVelocityChartHandler CreateHandler() => new(_dashRepo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ValidProject_ReturnsVelocityData()
    {
        var sprints = new List<VelocitySprintDto>
        {
            new(Guid.NewGuid(), "Sprint 1", 20, 18, 18, 18, 18),
            new(Guid.NewGuid(), "Sprint 2", 25, 22, 22, 20, 20),
        };
        _dashRepo.GetVelocityDataAsync(ProjectId, 10, Arg.Any<CancellationToken>())
            .Returns(new VelocityChartDto(sprints));

        var result = await CreateHandler().Handle(new GetVelocityChartQuery(ProjectId, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Sprints.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(new GetVelocityChartQuery(ProjectId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
