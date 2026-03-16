using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Reports.GetTeamHealthSummary;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Reports;

public sealed class GetTeamHealthSummaryTests
{
    private readonly ITeamHealthSummaryRepository _repo = Substitute.For<ITeamHealthSummaryRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public GetTeamHealthSummaryTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private GetTeamHealthSummaryHandler CreateHandler() => new(_repo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_SummaryExists_ReturnsLatestSummary()
    {
        var summary = new TeamHealthSummary
        {
            ProjectId = ProjectId,
            PeriodStart = new DateOnly(2026, 3, 1),
            PeriodEnd = new DateOnly(2026, 3, 14),
            SummaryData = JsonDocument.Parse("""{"morale":4.2,"velocity_trend":"up"}"""),
        };
        _repo.GetLatestByProjectAsync(ProjectId, Arg.Any<CancellationToken>()).Returns(summary);

        var result = await CreateHandler().Handle(new GetTeamHealthSummaryQuery(ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProjectId.Should().Be(ProjectId);
        result.Value.PeriodStart.Should().Be(new DateOnly(2026, 3, 1));
        result.Value.PeriodEnd.Should().Be(new DateOnly(2026, 3, 14));
    }

    [Fact]
    public async Task Handle_NoSummary_ReturnsNotFound()
    {
        _repo.GetLatestByProjectAsync(ProjectId, Arg.Any<CancellationToken>()).Returns((TeamHealthSummary?)null);

        var result = await CreateHandler().Handle(new GetTeamHealthSummaryQuery(ProjectId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(new GetTeamHealthSummaryQuery(ProjectId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
