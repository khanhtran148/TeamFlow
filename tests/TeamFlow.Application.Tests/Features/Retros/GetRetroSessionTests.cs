using FluentAssertions;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Retros.GetPreviousActionItems;
using TeamFlow.Application.Features.Retros.GetRetroSession;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Retros;

public sealed class GetRetroSessionTests
{
    private readonly IRetroSessionRepository _retroRepo = Substitute.For<IRetroSessionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public GetRetroSessionTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    [Fact]
    public async Task GetSession_Anonymous_StripsAuthorInfo()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .WithStatus(RetroSessionStatus.Open)
            .Anonymous()
            .Build();
        session.Facilitator = new User { Name = "Facilitator" };
        session.Cards =
        [
            new RetroCard
            {
                AuthorId = Guid.NewGuid(),
                Author = new User { Name = "Author" },
                Category = RetroCardCategory.WentWell,
                Content = "Good work",
                Votes = []
            }
        ];
        session.ActionItems = [];

        _retroRepo.GetByIdWithDetailsAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new GetRetroSessionHandler(_retroRepo, _currentUser, _permissions);
        var result = await handler.Handle(new GetRetroSessionQuery(session.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Cards.Should().HaveCount(1);
        result.Value.Cards[0].AuthorId.Should().BeNull();
        result.Value.Cards[0].AuthorName.Should().BeNull();
    }

    [Fact]
    public async Task GetSession_Public_IncludesAuthorInfo()
    {
        var authorId = Guid.NewGuid();
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .WithStatus(RetroSessionStatus.Open)
            .Build();
        session.Facilitator = new User { Name = "Facilitator" };
        session.Cards =
        [
            new RetroCard
            {
                AuthorId = authorId,
                Author = new User { Name = "Author" },
                Category = RetroCardCategory.WentWell,
                Content = "Good work",
                Votes = []
            }
        ];
        session.ActionItems = [];

        _retroRepo.GetByIdWithDetailsAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new GetRetroSessionHandler(_retroRepo, _currentUser, _permissions);
        var result = await handler.Handle(new GetRetroSessionQuery(session.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Cards[0].AuthorId.Should().Be(authorId);
        result.Value.Cards[0].AuthorName.Should().Be("Author");
    }

    [Fact]
    public async Task GetPreviousActions_NoPreviousSession_ReturnsEmptyList()
    {
        _retroRepo.GetLastClosedByProjectAsync(ProjectId, Arg.Any<CancellationToken>())
            .Returns((RetroSession?)null);

        var handler = new GetPreviousActionItemsHandler(_retroRepo, _currentUser, _permissions);
        var result = await handler.Handle(new GetPreviousActionItemsQuery(ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPreviousActions_WithClosedSession_ReturnsActionItems()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .WithStatus(RetroSessionStatus.Closed)
            .Build();
        session.ActionItems =
        [
            new RetroActionItem { Title = "Fix CI", SessionId = session.Id }
        ];

        _retroRepo.GetLastClosedByProjectAsync(ProjectId, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new GetPreviousActionItemsHandler(_retroRepo, _currentUser, _permissions);
        var result = await handler.Handle(new GetPreviousActionItemsQuery(ProjectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Title.Should().Be("Fix CI");
    }
}
