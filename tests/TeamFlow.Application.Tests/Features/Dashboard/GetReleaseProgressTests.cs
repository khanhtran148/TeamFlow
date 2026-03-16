using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.Dtos;
using TeamFlow.Application.Features.Dashboard.GetReleaseProgress;

namespace TeamFlow.Application.Tests.Features.Dashboard;

public sealed class GetReleaseProgressTests
{
    private readonly IDashboardRepository _dashRepo = Substitute.For<IDashboardRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid ReleaseId = Guid.NewGuid();

    public GetReleaseProgressTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private GetReleaseProgressHandler CreateHandler() => new(_dashRepo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ValidRelease_ReturnsProgressCounts()
    {
        var progress = new ReleaseProgressDto(12, 5, 8, 36.0m, 75.0m, 0.48);
        _dashRepo.GetReleaseProgressAsync(ReleaseId, Arg.Any<CancellationToken>())
            .Returns(progress);

        var result = await CreateHandler().Handle(new GetReleaseProgressQuery(ReleaseId, ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DoneCount.Should().Be(12);
        result.Value.InProgressCount.Should().Be(5);
        result.Value.TodoCount.Should().Be(8);
        result.Value.DonePoints.Should().Be(36.0m);
        result.Value.TotalPoints.Should().Be(75.0m);
        result.Value.CompletionPct.Should().BeApproximately(0.48, 0.001);
    }

    [Fact]
    public async Task Handle_EmptyRelease_ReturnsZeroCounts()
    {
        var progress = new ReleaseProgressDto(0, 0, 0, 0m, 0m, 0.0);
        _dashRepo.GetReleaseProgressAsync(ReleaseId, Arg.Any<CancellationToken>())
            .Returns(progress);

        var result = await CreateHandler().Handle(new GetReleaseProgressQuery(ReleaseId, ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.DoneCount.Should().Be(0);
        result.Value.TotalPoints.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(new GetReleaseProgressQuery(ReleaseId, ProjectId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
