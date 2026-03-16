using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Retros.ListRetroSessions;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Retros;

public sealed class ListRetroSessionsTests
{
    private readonly IRetroSessionRepository _retroRepo = Substitute.For<IRetroSessionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public ListRetroSessionsTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private ListRetroSessionsHandler CreateHandler() =>
        new(_retroRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_ReturnsPagedResults()
    {
        var session1 = RetroSessionBuilder.New().WithProject(ProjectId).Build();
        session1.Facilitator = new User { Name = "Alice" };
        var session2 = RetroSessionBuilder.New().WithProject(ProjectId).Build();
        session2.Facilitator = new User { Name = "Bob" };

        _retroRepo.ListByProjectAsync(ProjectId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((new[] { session1, session2 }.AsEnumerable(), 2));

        var result = await CreateHandler().Handle(
            new ListRetroSessionsQuery(ProjectId, 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 5)]
    public async Task Handle_PassesPaginationParameters(int page, int pageSize)
    {
        _retroRepo.ListByProjectAsync(ProjectId, page, pageSize, Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<RetroSession>(), 0));

        var result = await CreateHandler().Handle(
            new ListRetroSessionsQuery(ProjectId, page, pageSize), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(page);
        result.Value.PageSize.Should().Be(pageSize);
        await _retroRepo.Received(1).ListByProjectAsync(ProjectId, page, pageSize, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoPermission_ReturnsAccessDenied()
    {
        _permissions.HasPermissionAsync(UserId, ProjectId, Permission.Retro_View, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(
            new ListRetroSessionsQuery(ProjectId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }

    [Fact]
    public async Task Handle_EmptyProject_ReturnsEmptyList()
    {
        _retroRepo.ListByProjectAsync(ProjectId, 1, 20, Arg.Any<CancellationToken>())
            .Returns((Enumerable.Empty<RetroSession>(), 0));

        var result = await CreateHandler().Handle(
            new ListRetroSessionsQuery(ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }
}
