using FluentAssertions;
using TeamFlow.Application.Features.Retros.CastRetroVote;
using TeamFlow.Application.Features.Retros.CloseRetroSession;
using TeamFlow.Application.Features.Retros.MarkCardDiscussed;
using TeamFlow.Application.Features.Retros.StartRetroSession;
using TeamFlow.Application.Features.Retros.SubmitRetroCard;
using TeamFlow.Application.Features.Retros.TransitionRetroSession;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.Retros;

[Collection("Social")]
public sealed class RetroLifecycleTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    private async Task<RetroSession> SeedSessionAsync(
        RetroSessionStatus status = RetroSessionStatus.Draft,
        Guid? facilitatorId = null,
        bool anonymous = false)
    {
        var project = await SeedProjectAsync();
        var builder = RetroSessionBuilder.New()
            .WithProject(project.Id)
            .WithFacilitator(facilitatorId ?? SeedUserId)
            .WithStatus(status);
        if (anonymous) builder.Anonymous();
        var session = builder.Build();
        DbContext.Set<RetroSession>().Add(session);
        await DbContext.SaveChangesAsync();
        return session;
    }

    // --- StartRetroSession ---

    [Fact]
    public async Task Start_DraftSession_TransitionsToOpen()
    {
        var session = await SeedSessionAsync(RetroSessionStatus.Draft);

        var result = await Sender.Send(new StartRetroSessionCommand(session.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(RetroSessionStatus.Open);
    }

    [Fact]
    public async Task Start_NonDraftSession_ReturnsFailure()
    {
        var session = await SeedSessionAsync(RetroSessionStatus.Open);

        var result = await Sender.Send(new StartRetroSessionCommand(session.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Draft");
    }

    [Fact]
    public async Task Start_NotFacilitator_ReturnsFailure()
    {
        var otherFacilitator = UserBuilder.New().WithEmail("retro-start-facilitator@example.com").Build();
        DbContext.Users.Add(otherFacilitator);
        await DbContext.SaveChangesAsync();

        var session = await SeedSessionAsync(RetroSessionStatus.Draft, otherFacilitator.Id);

        var result = await Sender.Send(new StartRetroSessionCommand(session.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("facilitator");
    }

    // --- TransitionRetroSession ---

    [Theory]
    [InlineData(RetroSessionStatus.Open, RetroSessionStatus.Voting)]
    [InlineData(RetroSessionStatus.Voting, RetroSessionStatus.Discussing)]
    public async Task Transition_ValidTransition_Succeeds(RetroSessionStatus from, RetroSessionStatus to)
    {
        var session = await SeedSessionAsync(from);

        var result = await Sender.Send(new TransitionRetroSessionCommand(session.Id, to));

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
        var session = await SeedSessionAsync(from);

        var result = await Sender.Send(new TransitionRetroSessionCommand(session.Id, to));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid transition");
    }

    // --- SubmitRetroCard ---

    [Fact]
    public async Task SubmitCard_OpenSession_Succeeds()
    {
        var session = await SeedSessionAsync(RetroSessionStatus.Open);

        var result = await Sender.Send(
            new SubmitRetroCardCommand(session.Id, RetroCardCategory.WentWell, "Great sprint!"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Be("Great sprint!");
        result.Value.Category.Should().Be(RetroCardCategory.WentWell);
    }

    [Fact]
    public async Task SubmitCard_NonOpenSession_ReturnsFailure()
    {
        var session = await SeedSessionAsync(RetroSessionStatus.Voting);

        var result = await Sender.Send(
            new SubmitRetroCardCommand(session.Id, RetroCardCategory.WentWell, "Card"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Open");
    }

    [Fact]
    public async Task SubmitCard_AnonymousSession_StripsAuthorFromDto()
    {
        var session = await SeedSessionAsync(RetroSessionStatus.Open, anonymous: true);

        var result = await Sender.Send(
            new SubmitRetroCardCommand(session.Id, RetroCardCategory.NeedsImprovement, "Improve CI"));

        result.IsSuccess.Should().BeTrue();
        result.Value.AuthorId.Should().BeNull();
        result.Value.AuthorName.Should().BeNull();
    }

    // --- CastRetroVote ---

    [Fact]
    public async Task CastVote_VotingSession_Succeeds()
    {
        var session = await SeedSessionAsync(RetroSessionStatus.Voting);
        var card = new RetroCard
        {
            SessionId = session.Id,
            AuthorId = SeedUserId,
            Category = RetroCardCategory.WentWell,
            Content = "Card"
        };
        DbContext.Set<RetroCard>().Add(card);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new CastRetroVoteCommand(card.Id, 1));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CastVote_ExceedsMaxPerSession_ReturnsFailure()
    {
        var session = await SeedSessionAsync(RetroSessionStatus.Voting);
        var cards = Enumerable.Range(1, 5).Select(_ => new RetroCard
        {
            SessionId = session.Id,
            AuthorId = SeedUserId,
            Category = RetroCardCategory.WentWell,
            Content = "Card"
        }).ToList();
        DbContext.Set<RetroCard>().AddRange(cards);
        await DbContext.SaveChangesAsync();

        // Cast 5 votes (assumed max)
        foreach (var c in cards)
        {
            DbContext.Set<RetroVote>().Add(new RetroVote { CardId = c.Id, VoterId = SeedUserId, VoteCount = 1 });
        }
        await DbContext.SaveChangesAsync();

        // newCard author must be a real user to satisfy FK
        var newCard = new RetroCard
        {
            SessionId = session.Id,
            AuthorId = SeedUserId,
            Category = RetroCardCategory.NeedsImprovement,
            Content = "Another card"
        };
        DbContext.Set<RetroCard>().Add(newCard);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new CastRetroVoteCommand(newCard.Id, 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("maximum");
    }

    [Fact]
    public async Task CastVote_DuplicateVote_ReturnsFailure()
    {
        var session = await SeedSessionAsync(RetroSessionStatus.Voting);
        var card = new RetroCard
        {
            SessionId = session.Id,
            AuthorId = SeedUserId,
            Category = RetroCardCategory.WentWell,
            Content = "Card"
        };
        DbContext.Set<RetroCard>().Add(card);
        await DbContext.SaveChangesAsync();

        DbContext.Set<RetroVote>().Add(new RetroVote { CardId = card.Id, VoterId = SeedUserId, VoteCount = 1 });
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new CastRetroVoteCommand(card.Id, 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already voted");
    }

    [Fact]
    public async Task CastVote_NotVotingPhase_ReturnsFailure()
    {
        var session = await SeedSessionAsync(RetroSessionStatus.Open);
        var card = new RetroCard
        {
            SessionId = session.Id,
            AuthorId = SeedUserId,
            Category = RetroCardCategory.WentWell,
            Content = "Card"
        };
        DbContext.Set<RetroCard>().Add(card);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new CastRetroVoteCommand(card.Id, 1));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Voting");
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)3)]
    public async Task CastVote_InvalidVoteCount_ReturnsFailure(short voteCount)
    {
        var result = await Sender.Send(new CastRetroVoteCommand(Guid.NewGuid(), voteCount));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("VoteCount");
    }

    // --- MarkCardDiscussed ---

    [Fact]
    public async Task MarkDiscussed_DiscussingPhase_Succeeds()
    {
        var session = await SeedSessionAsync(RetroSessionStatus.Discussing);
        var card = new RetroCard
        {
            SessionId = session.Id,
            AuthorId = SeedUserId,
            Category = RetroCardCategory.WentWell,
            Content = "Card"
        };
        DbContext.Set<RetroCard>().Add(card);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new MarkCardDiscussedCommand(card.Id));

        result.IsSuccess.Should().BeTrue();
        card.IsDiscussed.Should().BeTrue();
    }

    [Fact]
    public async Task MarkDiscussed_NotDiscussingPhase_ReturnsFailure()
    {
        var session = await SeedSessionAsync(RetroSessionStatus.Voting);
        var card = new RetroCard
        {
            SessionId = session.Id,
            AuthorId = SeedUserId,
            Category = RetroCardCategory.WentWell,
            Content = "Card"
        };
        DbContext.Set<RetroCard>().Add(card);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new MarkCardDiscussedCommand(card.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Discussing");
    }

    // --- CloseRetroSession ---

    [Fact]
    public async Task Close_DiscussingSession_GeneratesSummaryAndCloses()
    {
        var session = await SeedSessionAsync(RetroSessionStatus.Discussing);
        DbContext.Set<RetroCard>().AddRange(
            new RetroCard { SessionId = session.Id, AuthorId = SeedUserId, Category = RetroCardCategory.WentWell, Content = "Good" },
            new RetroCard { SessionId = session.Id, AuthorId = SeedUserId, Category = RetroCardCategory.NeedsImprovement, Content = "Bad" }
        );
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new CloseRetroSessionCommand(session.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(RetroSessionStatus.Closed);
        result.Value.AiSummary.Should().NotBeNull();
    }

    [Fact]
    public async Task Close_NotDiscussingSession_ReturnsFailure()
    {
        var session = await SeedSessionAsync(RetroSessionStatus.Voting);

        var result = await Sender.Send(new CloseRetroSessionCommand(session.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Discussing");
    }
}
