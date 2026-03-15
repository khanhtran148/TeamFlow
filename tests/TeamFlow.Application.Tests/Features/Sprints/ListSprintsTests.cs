using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Sprints.ListSprints;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Sprints;

public sealed class ListSprintsTests
{
    private readonly ISprintRepository _sprintRepo = Substitute.For<ISprintRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid ActorId = Guid.NewGuid();

    public ListSprintsTests()
    {
        _currentUser.Id.Returns(ActorId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private ListSprintsHandler CreateHandler() =>
        new(_sprintRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ReturnsPaginatedSprints()
    {
        var projectId = Guid.NewGuid();
        var sprint1 = SprintBuilder.New().WithProject(projectId).WithName("Sprint 1").Build();
        var sprint2 = SprintBuilder.New().WithProject(projectId).WithName("Sprint 2").Build();

        _sprintRepo.ListByProjectPagedAsync(projectId, 1, 20, Arg.Any<CancellationToken>())
            .Returns(([sprint1, sprint2], 2));

        var query = new ListSprintsQuery(projectId);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        var projectId = Guid.NewGuid();
        _permissions.HasPermissionAsync(ActorId, projectId, Permission.Project_View, Arg.Any<CancellationToken>())
            .Returns(false);

        var query = new ListSprintsQuery(projectId);
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
