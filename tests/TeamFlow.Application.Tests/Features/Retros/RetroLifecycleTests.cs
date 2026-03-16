using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Retros.CastRetroVote;
using TeamFlow.Application.Features.Retros.CloseRetroSession;
using TeamFlow.Application.Features.Retros.MarkCardDiscussed;
using TeamFlow.Application.Features.Retros.StartRetroSession;
using TeamFlow.Application.Features.Retros.SubmitRetroCard;
using TeamFlow.Application.Features.Retros.TransitionRetroSession;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Retros;

public sealed class RetroLifecycleTests
{
    private readonly IRetroSessionRepository _retroRepo = Substitute.For<IRetroSessionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public RetroLifecycleTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _retroRepo.UpdateAsync(Arg.Any<RetroSession>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<RetroSession>());
    }

    // --- StartRetroSession ---

    [Fact]
    public async Task Start_DraftSession_TransitionsToOpen()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .WithStatus(RetroSessionStatus.Draft)
            .Build();
        _retroRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new StartRetroSessionHandler(_retroRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new StartRetroSessionCommand(session.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(RetroSessionStatus.Open);
    }

    [Fact]
    public async Task Start_NonDraftSession_ReturnsFailure()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .WithStatus(RetroSessionStatus.Open)
            .Build();
        _retroRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new StartRetroSessionHandler(_retroRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new StartRetroSessionCommand(session.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Draft");
    }

    [Fact]
    public async Task Start_NotFacilitator_ReturnsFailure()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(Guid.NewGuid())
            .WithStatus(RetroSessionStatus.Draft)
            .Build();
        _retroRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new StartRetroSessionHandler(_retroRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new StartRetroSessionCommand(session.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("facilitator");
    }

    // --- TransitionRetroSession ---

    [Theory]
    [InlineData(RetroSessionStatus.Open, RetroSessionStatus.Voting)]
    [InlineData(RetroSessionStatus.Voting, RetroSessionStatus.Discussing)]
    public async Task Transition_ValidTransition_Succeeds(RetroSessionStatus from, RetroSessionStatus to)
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .WithStatus(from)
            .Build();
        _retroRepo.GetByIdWithDetailsAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new TransitionRetroSessionHandler(_retroRepo, _currentUser, _permissions);
        var result = await handler.Handle(new TransitionRetroSessionCommand(session.Id, to), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(RetroSessionStatus.Draft, RetroSessionStatus.Voting)]
    [InlineData(RetroSessionStatus.Open, RetroSessionStatus.Discussing)]
    [InlineData(RetroSessionStatus.Voting, RetroSessionStatus.Open)]
    [InlineData(RetroSessionStatus.Discussing, RetroSessionStatus.Open)]
    public async Task Transition_InvalidTransition_ReturnsFailure(RetroSessionStatus from, RetroSessionStatus to)
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .WithStatus(from)
            .Build();
        _retroRepo.GetByIdWithDetailsAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new TransitionRetroSessionHandler(_retroRepo, _currentUser, _permissions);
        var result = await handler.Handle(new TransitionRetroSessionCommand(session.Id, to), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid transition");
    }

    // --- SubmitRetroCard ---

    [Fact]
    public async Task SubmitCard_OpenSession_Succeeds()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .WithStatus(RetroSessionStatus.Open)
            .Build();
        _retroRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _retroRepo.AddCardAsync(Arg.Any<RetroCard>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<RetroCard>());

        var handler = new SubmitRetroCardHandler(_retroRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(
            new SubmitRetroCardCommand(session.Id, RetroCardCategory.WentWell, "Great sprint!"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Be("Great sprint!");
        result.Value.Category.Should().Be(RetroCardCategory.WentWell);
    }

    [Fact]
    public async Task SubmitCard_NonOpenSession_ReturnsFailure()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithStatus(RetroSessionStatus.Voting)
            .Build();
        _retroRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new SubmitRetroCardHandler(_retroRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(
            new SubmitRetroCardCommand(session.Id, RetroCardCategory.WentWell, "Card"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Open");
    }

    [Fact]
    public async Task SubmitCard_AnonymousSession_StripsAuthorFromDto()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .WithStatus(RetroSessionStatus.Open)
            .Anonymous()
            .Build();
        _retroRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _retroRepo.AddCardAsync(Arg.Any<RetroCard>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<RetroCard>());

        var handler = new SubmitRetroCardHandler(_retroRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(
            new SubmitRetroCardCommand(session.Id, RetroCardCategory.NeedsImprovement, "Improve CI"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AuthorId.Should().BeNull();
        result.Value.AuthorName.Should().BeNull();
    }

    // --- CastRetroVote ---

    [Fact]
    public async Task CastVote_VotingSession_Succeeds()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithStatus(RetroSessionStatus.Voting)
            .Build();
        var card = new RetroCard { SessionId = session.Id, Session = session, Content = "Card", Votes = [] };

        _retroRepo.GetCardByIdAsync(card.Id, Arg.Any<CancellationToken>()).Returns(card);
        _retroRepo.GetVoteAsync(card.Id, UserId, Arg.Any<CancellationToken>()).Returns((RetroVote?)null);
        _retroRepo.GetTotalVoteCountForUserInSessionAsync(session.Id, UserId, Arg.Any<CancellationToken>()).Returns(0);
        _retroRepo.AddVoteAsync(Arg.Any<RetroVote>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<RetroVote>());

        var handler = new CastRetroVoteHandler(_retroRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new CastRetroVoteCommand(card.Id, 1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CastVote_ExceedsMaxPerSession_ReturnsFailure()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithStatus(RetroSessionStatus.Voting)
            .Build();
        var card = new RetroCard { SessionId = session.Id, Session = session, Content = "Card", Votes = [] };

        _retroRepo.GetCardByIdAsync(card.Id, Arg.Any<CancellationToken>()).Returns(card);
        _retroRepo.GetVoteAsync(card.Id, UserId, Arg.Any<CancellationToken>()).Returns((RetroVote?)null);
        _retroRepo.GetTotalVoteCountForUserInSessionAsync(session.Id, UserId, Arg.Any<CancellationToken>()).Returns(5);

        var handler = new CastRetroVoteHandler(_retroRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new CastRetroVoteCommand(card.Id, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("maximum");
    }

    [Fact]
    public async Task CastVote_DuplicateVote_ReturnsFailure()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithStatus(RetroSessionStatus.Voting)
            .Build();
        var card = new RetroCard { SessionId = session.Id, Session = session, Content = "Card", Votes = [] };

        _retroRepo.GetCardByIdAsync(card.Id, Arg.Any<CancellationToken>()).Returns(card);
        _retroRepo.GetVoteAsync(card.Id, UserId, Arg.Any<CancellationToken>())
            .Returns(new RetroVote { CardId = card.Id, VoterId = UserId });

        var handler = new CastRetroVoteHandler(_retroRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new CastRetroVoteCommand(card.Id, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already voted");
    }

    [Fact]
    public async Task CastVote_NotVotingPhase_ReturnsFailure()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithStatus(RetroSessionStatus.Open)
            .Build();
        var card = new RetroCard { SessionId = session.Id, Session = session, Content = "Card", Votes = [] };

        _retroRepo.GetCardByIdAsync(card.Id, Arg.Any<CancellationToken>()).Returns(card);
        _retroRepo.GetVoteAsync(card.Id, UserId, Arg.Any<CancellationToken>()).Returns((RetroVote?)null);

        var handler = new CastRetroVoteHandler(_retroRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new CastRetroVoteCommand(card.Id, 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Voting");
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)3)]
    public async Task CastVote_InvalidVoteCount_ReturnsFailure(short voteCount)
    {
        var handler = new CastRetroVoteHandler(_retroRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new CastRetroVoteCommand(Guid.NewGuid(), voteCount), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("VoteCount");
    }

    // --- MarkCardDiscussed ---

    [Fact]
    public async Task MarkDiscussed_DiscussingPhase_Succeeds()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .WithStatus(RetroSessionStatus.Discussing)
            .Build();
        var card = new RetroCard { SessionId = session.Id, Session = session, Content = "Card", Votes = [] };

        _retroRepo.GetCardByIdAsync(card.Id, Arg.Any<CancellationToken>()).Returns(card);
        _retroRepo.UpdateCardAsync(Arg.Any<RetroCard>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<RetroCard>());

        var handler = new MarkCardDiscussedHandler(_retroRepo, _currentUser, _permissions);
        var result = await handler.Handle(new MarkCardDiscussedCommand(card.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        card.IsDiscussed.Should().BeTrue();
    }

    [Fact]
    public async Task MarkDiscussed_NotDiscussingPhase_ReturnsFailure()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .WithStatus(RetroSessionStatus.Voting)
            .Build();
        var card = new RetroCard { SessionId = session.Id, Session = session, Content = "Card", Votes = [] };

        _retroRepo.GetCardByIdAsync(card.Id, Arg.Any<CancellationToken>()).Returns(card);

        var handler = new MarkCardDiscussedHandler(_retroRepo, _currentUser, _permissions);
        var result = await handler.Handle(new MarkCardDiscussedCommand(card.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Discussing");
    }

    // --- CloseRetroSession ---

    [Fact]
    public async Task Close_DiscussingSession_GeneratesSummaryAndCloses()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .WithStatus(RetroSessionStatus.Discussing)
            .Build();
        session.Cards = [
            new RetroCard { Category = RetroCardCategory.WentWell, Content = "Good", Votes = [] },
            new RetroCard { Category = RetroCardCategory.NeedsImprovement, Content = "Bad", Votes = [] }
        ];
        session.ActionItems = [];

        _retroRepo.GetByIdWithDetailsAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new CloseRetroSessionHandler(_retroRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new CloseRetroSessionCommand(session.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(RetroSessionStatus.Closed);
        result.Value.AiSummary.Should().NotBeNull();
    }

    [Fact]
    public async Task Close_NotDiscussingSession_ReturnsFailure()
    {
        var session = RetroSessionBuilder.New()
            .WithProject(ProjectId)
            .WithFacilitator(UserId)
            .WithStatus(RetroSessionStatus.Voting)
            .Build();
        session.Cards = [];
        session.ActionItems = [];

        _retroRepo.GetByIdWithDetailsAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new CloseRetroSessionHandler(_retroRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new CloseRetroSessionCommand(session.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Discussing");
    }
}
