using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Reports.ListTeamHealthSummaries;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Reports;

public sealed class ListTeamHealthSummariesTests
{
    private readonly ITeamHealthSummaryRepository _repo = Substitute.For<ITeamHealthSummaryRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public ListTeamHealthSummariesTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(ActorId, ProjectId, Permission.Project_View, Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private ListTeamHealthSummariesHandler CreateHandler() => new(_repo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_HasSummaries_ReturnsPaginatedList()
    {
        var summaries = new List<TeamHealthSummary>
        {
            new()
            {
                ProjectId = ProjectId,
                PeriodStart = new DateOnly(2026, 3, 1),
                PeriodEnd = new DateOnly(2026, 3, 14),
                SummaryData = JsonDocument.Parse("""{"morale":8}""")
            },
            new()
            {
                ProjectId = ProjectId,
                PeriodStart = new DateOnly(2026, 3, 15),
                PeriodEnd = new DateOnly(2026, 3, 28),
                SummaryData = JsonDocument.Parse("""{"morale":7}""")
            }
        };
        _repo.ListByProjectAsync(ProjectId, 1, 10, Arg.Any<CancellationToken>())
            .Returns((summaries.AsEnumerable(), 2));

        var result = await CreateHandler().Handle(
            new ListTeamHealthSummariesQuery(ProjectId, 1, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Page.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NoSummaries_ReturnsEmptyPagedResult()
    {
        _repo.ListByProjectAsync(ProjectId, 1, 10, Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<TeamHealthSummary>(), 0));

        var result = await CreateHandler().Handle(
            new ListTeamHealthSummariesQuery(ProjectId, 1, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        _permissions.HasPermissionAsync(ActorId, ProjectId, Permission.Project_View, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(
            new ListTeamHealthSummariesQuery(ProjectId, 1, 10), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
