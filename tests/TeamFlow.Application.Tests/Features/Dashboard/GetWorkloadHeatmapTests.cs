using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.Dtos;
using TeamFlow.Application.Features.Dashboard.GetWorkloadHeatmap;

namespace TeamFlow.Application.Tests.Features.Dashboard;

public sealed class GetWorkloadHeatmapTests
{
    private readonly IDashboardRepository _dashRepo = Substitute.For<IDashboardRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public GetWorkloadHeatmapTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private GetWorkloadHeatmapHandler CreateHandler() => new(_dashRepo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ValidProject_ReturnsPerMemberWorkload()
    {
        var members = new List<WorkloadMemberDto>
        {
            new(Guid.NewGuid(), "Alice", 8, 3, 21.0m),
            new(Guid.NewGuid(), "Bob", 5, 2, 13.0m),
        };
        _dashRepo.GetWorkloadDataAsync(ProjectId, Arg.Any<CancellationToken>())
            .Returns(new WorkloadHeatmapDto(members));

        var result = await CreateHandler().Handle(new GetWorkloadHeatmapQuery(ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Members.Should().HaveCount(2);
        result.Value.Members[0].AssignedCount.Should().Be(8);
        result.Value.Members[0].InProgressCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_EmptyProject_ReturnsEmptyMembers()
    {
        _dashRepo.GetWorkloadDataAsync(ProjectId, Arg.Any<CancellationToken>())
            .Returns(new WorkloadHeatmapDto([]));

        var result = await CreateHandler().Handle(new GetWorkloadHeatmapQuery(ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Members.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(new GetWorkloadHeatmapQuery(ProjectId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
