using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Dashboard.Dtos;
using TeamFlow.Application.Features.Dashboard.GetDashboardSummary;

namespace TeamFlow.Application.Tests.Features.Dashboard;

public sealed class GetDashboardSummaryTests
{
    private readonly IDashboardRepository _dashRepo = Substitute.For<IDashboardRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public GetDashboardSummaryTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private GetDashboardSummaryHandler CreateHandler() => new(_dashRepo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ValidProject_ReturnsSummary()
    {
        var summary = new DashboardSummaryDto(
            Guid.NewGuid(), "Sprint 14", 82, 34, 0.585, 1, 3, 36.0m);
        _dashRepo.GetDashboardSummaryAsync(ProjectId, Arg.Any<CancellationToken>())
            .Returns(summary);

        var result = await CreateHandler().Handle(new GetDashboardSummaryQuery(ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalItems.Should().Be(82);
        result.Value.CompletionPct.Should().BeApproximately(0.585, 0.001);
    }
}
