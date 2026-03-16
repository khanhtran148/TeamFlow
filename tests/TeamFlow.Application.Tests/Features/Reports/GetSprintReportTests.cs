using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Reports.GetSprintReport;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Reports;

public sealed class GetSprintReportTests
{
    private readonly ISprintReportRepository _repo = Substitute.For<ISprintReportRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();
    private static readonly Guid SprintId = Guid.NewGuid();

    public GetSprintReportTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private GetSprintReportHandler CreateHandler() => new(_repo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ReportExists_ReturnsReport()
    {
        var report = new SprintReport
        {
            SprintId = SprintId,
            ProjectId = ProjectId,
            ReportData = JsonDocument.Parse("""{"velocity":35}"""),
            GeneratedBy = "System"
        };
        _repo.GetBySprintIdAsync(SprintId, Arg.Any<CancellationToken>()).Returns(report);

        var result = await CreateHandler().Handle(new GetSprintReportQuery(SprintId, ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.SprintId.Should().Be(SprintId);
    }

    [Fact]
    public async Task Handle_ReportNotFound_ReturnsNotFound()
    {
        _repo.GetBySprintIdAsync(SprintId, Arg.Any<CancellationToken>()).Returns((SprintReport?)null);

        var result = await CreateHandler().Handle(new GetSprintReportQuery(SprintId, ProjectId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
