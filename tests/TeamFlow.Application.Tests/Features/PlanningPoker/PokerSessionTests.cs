using FluentAssertions;
using TeamFlow.Application.Features.PlanningPoker.CastPokerVote;
using TeamFlow.Application.Features.PlanningPoker.ConfirmPokerEstimate;
using TeamFlow.Application.Features.PlanningPoker.CreatePokerSession;
using TeamFlow.Application.Features.PlanningPoker.GetPokerSession;
using TeamFlow.Application.Features.PlanningPoker.RevealPokerVotes;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.PlanningPoker;

[Collection("Social")]
public sealed class PokerSessionTests(PostgresCollectionFixture fixture)
    : ApplicationTestBase(fixture)
{
    // --- CreatePokerSession ---

    [Fact]
    public async Task Create_ValidWorkItem_CreatesSession()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);

        var result = await Sender.Send(new CreatePokerSessionCommand(workItem.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.WorkItemId.Should().Be(workItem.Id);
        result.Value.FacilitatorId.Should().Be(SeedUserId);
    }

    [Fact]
    public async Task Create_DuplicateActive_ReturnsConflict()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var existingSession = PlanningPokerSessionBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Build();
        DbContext.Set<PlanningPokerSession>().Add(existingSession);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new CreatePokerSessionCommand(workItem.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    // --- CastPokerVote ---

    [Fact]
    public async Task CastVote_ValidSession_Succeeds()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var session = PlanningPokerSessionBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Build();
        DbContext.Set<PlanningPokerSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new CastPokerVoteCommand(session.Id, 5));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CastVote_UpdateExisting_Succeeds()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var session = PlanningPokerSessionBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Build();
        DbContext.Set<PlanningPokerSession>().Add(session);
        await DbContext.SaveChangesAsync();

        DbContext.Set<PlanningPokerVote>().Add(new PlanningPokerVote
        {
            SessionId = session.Id,
            VoterId = SeedUserId,
            Value = 3
        });
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new CastPokerVoteCommand(session.Id, 8));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CastVote_ClosedSession_ReturnsFailure()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var session = PlanningPokerSessionBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Closed()
            .Build();
        DbContext.Set<PlanningPokerSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new CastPokerVoteCommand(session.Id, 5));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("closed");
    }

    [Theory]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(10)]
    public async Task Validate_InvalidFibonacci_Fails(decimal value)
    {
        var validator = new CastPokerVoteValidator();
        var cmd = new CastPokerVoteCommand(Guid.NewGuid(), value);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(13)]
    [InlineData(21)]
    public async Task Validate_ValidFibonacci_Passes(decimal value)
    {
        var validator = new CastPokerVoteValidator();
        var cmd = new CastPokerVoteCommand(Guid.NewGuid(), value);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    // --- RevealPokerVotes ---

    [Fact]
    public async Task Reveal_AsFacilitator_Succeeds()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var session = PlanningPokerSessionBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Build();
        DbContext.Set<PlanningPokerSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new RevealPokerVotesCommand(session.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsRevealed.Should().BeTrue();
    }

    [Fact]
    public async Task Reveal_NotFacilitator_ReturnsFailure()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var otherFacilitator = UserBuilder.New().WithEmail("poker-facilitator-reveal@example.com").Build();
        DbContext.Users.Add(otherFacilitator);
        await DbContext.SaveChangesAsync();

        var session = PlanningPokerSessionBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithProject(project.Id)
            .WithFacilitator(otherFacilitator.Id)
            .Build();
        DbContext.Set<PlanningPokerSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new RevealPokerVotesCommand(session.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("facilitator");
    }

    [Fact]
    public async Task Reveal_AlreadyRevealed_ReturnsFailure()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var session = PlanningPokerSessionBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Revealed()
            .Build();
        DbContext.Set<PlanningPokerSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new RevealPokerVotesCommand(session.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already been revealed");
    }

    // --- ConfirmPokerEstimate ---

    [Fact]
    public async Task Confirm_RevealedSession_ClosesAndUpdatesWorkItem()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var session = PlanningPokerSessionBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Revealed()
            .Build();
        DbContext.Set<PlanningPokerSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ConfirmPokerEstimateCommand(session.Id, 8));

        result.IsSuccess.Should().BeTrue();
        result.Value.FinalEstimate.Should().Be(8);
        result.Value.ClosedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Confirm_NotFacilitator_ReturnsFailure()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var otherFacilitator = UserBuilder.New().WithEmail("poker-facilitator-confirm@example.com").Build();
        DbContext.Users.Add(otherFacilitator);
        await DbContext.SaveChangesAsync();

        var session = PlanningPokerSessionBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithProject(project.Id)
            .WithFacilitator(otherFacilitator.Id)
            .Revealed()
            .Build();
        DbContext.Set<PlanningPokerSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ConfirmPokerEstimateCommand(session.Id, 8));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("facilitator");
    }

    [Theory]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(10)]
    public async Task ConfirmValidate_InvalidFibonacci_Fails(decimal value)
    {
        var validator = new ConfirmPokerEstimateValidator();
        var cmd = new ConfirmPokerEstimateCommand(Guid.NewGuid(), value);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(13)]
    [InlineData(21)]
    public async Task ConfirmValidate_ValidFibonacci_Passes(decimal value)
    {
        var validator = new ConfirmPokerEstimateValidator();
        var cmd = new ConfirmPokerEstimateCommand(Guid.NewGuid(), value);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmValidate_EmptySessionId_Fails()
    {
        var validator = new ConfirmPokerEstimateValidator();
        var cmd = new ConfirmPokerEstimateCommand(Guid.Empty, 5);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Confirm_NotRevealed_ReturnsFailure()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var session = PlanningPokerSessionBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Build();
        DbContext.Set<PlanningPokerSession>().Add(session);
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new ConfirmPokerEstimateCommand(session.Id, 8));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("revealed");
    }

    // --- GetPokerSession ---

    [Fact]
    public async Task Get_VotesHiddenBeforeReveal()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var session = PlanningPokerSessionBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Build();
        DbContext.Set<PlanningPokerSession>().Add(session);
        await DbContext.SaveChangesAsync();

        DbContext.Set<PlanningPokerVote>().Add(new PlanningPokerVote
        {
            SessionId = session.Id,
            VoterId = SeedUserId,
            Value = 5
        });
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetPokerSessionQuery(session.Id, null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Votes.Should().HaveCount(1);
        result.Value.Votes[0].Value.Should().BeNull();
    }

    [Fact]
    public async Task Get_VotesVisibleAfterReveal()
    {
        var project = await SeedProjectAsync();
        var workItem = await SeedWorkItemAsync(project.Id);
        var session = PlanningPokerSessionBuilder.New()
            .WithWorkItem(workItem.Id)
            .WithProject(project.Id)
            .WithFacilitator(SeedUserId)
            .Revealed()
            .Build();
        DbContext.Set<PlanningPokerSession>().Add(session);
        await DbContext.SaveChangesAsync();

        DbContext.Set<PlanningPokerVote>().Add(new PlanningPokerVote
        {
            SessionId = session.Id,
            VoterId = SeedUserId,
            Value = 5
        });
        await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetPokerSessionQuery(session.Id, null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Votes[0].Value.Should().Be(5);
    }
}
