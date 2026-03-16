using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Reports.ListSprintReports;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Tests.Features.Reports;

public sealed class ListSprintReportsTests
{
    private readonly ISprintReportRepository _repo = Substitute.For<ISprintReportRepository>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private static readonly Guid ActorId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public ListSprintReportsTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private ListSprintReportsHandler CreateHandler() => new(_repo, _permissions, _currentUser);

    [Fact]
    public async Task Handle_ValidProject_ReturnsPaginatedReports()
    {
        var reports = new[]
        {
            new SprintReport { SprintId = Guid.NewGuid(), ProjectId = ProjectId, ReportData = JsonDocument.Parse("{}"), GeneratedBy = "System" },
            new SprintReport { SprintId = Guid.NewGuid(), ProjectId = ProjectId, ReportData = JsonDocument.Parse("{}"), GeneratedBy = "System" },
        };
        _repo.ListByProjectAsync(ProjectId, 1, 10, Arg.Any<CancellationToken>())
            .Returns((reports.AsEnumerable(), 2));

        var result = await CreateHandler().Handle(new ListSprintReportsQuery(ProjectId, 1, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);
        result.Value.Page.Should().Be(1);
    }

    [Fact]
    public async Task Handle_EmptyProject_ReturnsEmptyPage()
    {
        _repo.ListByProjectAsync(ProjectId, 1, 10, Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<SprintReport>(), 0));

        var result = await CreateHandler().Handle(new ListSprintReportsQuery(ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(new ListSprintReportsQuery(ProjectId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
