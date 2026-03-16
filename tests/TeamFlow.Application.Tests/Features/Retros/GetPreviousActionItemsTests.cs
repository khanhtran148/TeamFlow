using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Retros.GetPreviousActionItems;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Retros;

public sealed class GetPreviousActionItemsTests
{
    private readonly IRetroSessionRepository _retroRepo = Substitute.For<IRetroSessionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public GetPreviousActionItemsTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    private GetPreviousActionItemsHandler CreateHandler() =>
        new(_retroRepo, _currentUser, _permissions);

    [Fact]
    public async Task Handle_WithClosedSession_ReturnsActionItems()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .WithStatus(RetroSessionStatus.Closed)
            .Build();
        session.ActionItems =
        [
            new RetroActionItem { Title = "Fix CI", SessionId = session.Id },
            new RetroActionItem { Title = "Update docs", SessionId = session.Id }
        ];

        _retroRepo.GetLastClosedByProjectAsync(ProjectId, Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateHandler().Handle(new GetPreviousActionItemsQuery(ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Title.Should().Be("Fix CI");
        result.Value[1].Title.Should().Be("Update docs");
    }

    [Fact]
    public async Task Handle_NoPreviousSessions_ReturnsEmptyList()
    {
        _retroRepo.GetLastClosedByProjectAsync(ProjectId, Arg.Any<CancellationToken>())
            .Returns((RetroSession?)null);

        var result = await CreateHandler().Handle(new GetPreviousActionItemsQuery(ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PreviousSessionWithNoActionItems_ReturnsEmptyList()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .WithStatus(RetroSessionStatus.Closed)
            .Build();
        session.ActionItems = [];

        _retroRepo.GetLastClosedByProjectAsync(ProjectId, Arg.Any<CancellationToken>()).Returns(session);

        var result = await CreateHandler().Handle(new GetPreviousActionItemsQuery(ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoRetroViewPermission_ReturnsForbidden()
    {
        _permissions.HasPermissionAsync(UserId, ProjectId, Permission.Retro_View, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await CreateHandler().Handle(new GetPreviousActionItemsQuery(ProjectId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access denied");
    }
}
