using FluentAssertions;
using MediatR;
using NSubstitute;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.PlanningPoker.CastPokerVote;
using TeamFlow.Application.Features.PlanningPoker.ConfirmPokerEstimate;
using TeamFlow.Application.Features.PlanningPoker.CreatePokerSession;
using TeamFlow.Application.Features.PlanningPoker.GetPokerSession;
using TeamFlow.Application.Features.PlanningPoker.RevealPokerVotes;
using TeamFlow.Domain.Entities;
using TeamFlow.Tests.Common.Builders;

namespace TeamFlow.Application.Tests.Features.PlanningPoker;

public sealed class PokerSessionTests
{
    private readonly IPlanningPokerSessionRepository _pokerRepo = Substitute.For<IPlanningPokerSessionRepository>();
    private readonly IWorkItemRepository _workItemRepo = Substitute.For<IWorkItemRepository>();
    private readonly IHistoryService _historyService = Substitute.For<IHistoryService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProjectId = Guid.NewGuid();

    public PokerSessionTests()
    {
        _currentUser.Id.Returns(UserId);
        _permissions.HasPermissionAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .Returns(true);
    }

    // --- CreatePokerSession ---

    [Fact]
    public async Task Create_ValidWorkItem_CreatesSession()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);
        _pokerRepo.GetActiveByWorkItemAsync(workItem.Id, Arg.Any<CancellationToken>())
            .Returns((PlanningPokerSession?)null);
        _pokerRepo.AddAsync(Arg.Any<PlanningPokerSession>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<PlanningPokerSession>());

        var handler = new CreatePokerSessionHandler(_pokerRepo, _workItemRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new CreatePokerSessionCommand(workItem.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WorkItemId.Should().Be(workItem.Id);
        result.Value.FacilitatorId.Should().Be(UserId);
    }

    [Fact]
    public async Task Create_DuplicateActive_ReturnsConflict()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);
        _pokerRepo.GetActiveByWorkItemAsync(workItem.Id, Arg.Any<CancellationToken>())
            .Returns(PlanningPokerSessionBuilder.New().WithWorkItem(workItem.Id).Build());

        var handler = new CreatePokerSessionHandler(_pokerRepo, _workItemRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new CreatePokerSessionCommand(workItem.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    // --- CastPokerVote ---

    [Fact]
    public async Task CastVote_ValidSession_Succeeds()
    {
        var session = PlanningPokerSessionBuilder.New().WithProject(ProjectId).Build();
        _pokerRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _pokerRepo.GetVoteAsync(session.Id, UserId, Arg.Any<CancellationToken>())
            .Returns((PlanningPokerVote?)null);
        _pokerRepo.AddVoteAsync(Arg.Any<PlanningPokerVote>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<PlanningPokerVote>());
        _pokerRepo.GetVoteCountAsync(session.Id, Arg.Any<CancellationToken>()).Returns(1);

        var handler = new CastPokerVoteHandler(_pokerRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new CastPokerVoteCommand(session.Id, 5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CastVote_UpdateExisting_Succeeds()
    {
        var session = PlanningPokerSessionBuilder.New().WithProject(ProjectId).Build();
        var existingVote = new PlanningPokerVote { SessionId = session.Id, VoterId = UserId, Value = 3 };

        _pokerRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _pokerRepo.GetVoteAsync(session.Id, UserId, Arg.Any<CancellationToken>()).Returns(existingVote);
        _pokerRepo.UpdateVoteAsync(Arg.Any<PlanningPokerVote>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<PlanningPokerVote>());
        _pokerRepo.GetVoteCountAsync(session.Id, Arg.Any<CancellationToken>()).Returns(1);

        var handler = new CastPokerVoteHandler(_pokerRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new CastPokerVoteCommand(session.Id, 8), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existingVote.Value.Should().Be(8);
    }

    [Fact]
    public async Task CastVote_ClosedSession_ReturnsFailure()
    {
        var session = PlanningPokerSessionBuilder.New().WithProject(ProjectId).Closed().Build();
        _pokerRepo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new CastPokerVoteHandler(_pokerRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new CastPokerVoteCommand(session.Id, 5), CancellationToken.None);

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
        var session = PlanningPokerSessionBuilder.New()
            .WithProject(ProjectId).WithFacilitator(UserId).Build();
        session.Votes = [];
        _pokerRepo.GetByIdWithVotesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _pokerRepo.UpdateAsync(Arg.Any<PlanningPokerSession>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<PlanningPokerSession>());

        var handler = new RevealPokerVotesHandler(_pokerRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new RevealPokerVotesCommand(session.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsRevealed.Should().BeTrue();
    }

    [Fact]
    public async Task Reveal_NotFacilitator_ReturnsFailure()
    {
        var session = PlanningPokerSessionBuilder.New()
            .WithProject(ProjectId).WithFacilitator(Guid.NewGuid()).Build();
        _pokerRepo.GetByIdWithVotesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new RevealPokerVotesHandler(_pokerRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new RevealPokerVotesCommand(session.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("facilitator");
    }

    [Fact]
    public async Task Reveal_AlreadyRevealed_ReturnsFailure()
    {
        var session = PlanningPokerSessionBuilder.New()
            .WithProject(ProjectId).WithFacilitator(UserId).Revealed().Build();
        _pokerRepo.GetByIdWithVotesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new RevealPokerVotesHandler(_pokerRepo, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new RevealPokerVotesCommand(session.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already been revealed");
    }

    // --- ConfirmPokerEstimate ---

    [Fact]
    public async Task Confirm_RevealedSession_ClosesAndUpdatesWorkItem()
    {
        var workItem = WorkItemBuilder.New().WithProject(ProjectId).Build();
        var session = PlanningPokerSessionBuilder.New()
            .WithProject(ProjectId).WithFacilitator(UserId).WithWorkItem(workItem.Id).Revealed().Build();
        session.Votes = [];

        _pokerRepo.GetByIdWithVotesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _pokerRepo.UpdateAsync(Arg.Any<PlanningPokerSession>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<PlanningPokerSession>());
        _workItemRepo.GetByIdAsync(workItem.Id, Arg.Any<CancellationToken>()).Returns(workItem);
        _workItemRepo.UpdateAsync(Arg.Any<WorkItem>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<WorkItem>());

        var handler = new ConfirmPokerEstimateHandler(
            _pokerRepo, _workItemRepo, _historyService, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new ConfirmPokerEstimateCommand(session.Id, 8), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FinalEstimate.Should().Be(8);
        result.Value.ClosedAt.Should().NotBeNull();
        workItem.EstimationValue.Should().Be(8);
        workItem.EstimationSource.Should().Be("Poker");
    }

    [Fact]
    public async Task Confirm_NotFacilitator_ReturnsFailure()
    {
        var session = PlanningPokerSessionBuilder.New()
            .WithProject(ProjectId).WithFacilitator(Guid.NewGuid()).Revealed().Build();
        session.Votes = [];
        _pokerRepo.GetByIdWithVotesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new ConfirmPokerEstimateHandler(
            _pokerRepo, _workItemRepo, _historyService, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new ConfirmPokerEstimateCommand(session.Id, 8), CancellationToken.None);

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
        var session = PlanningPokerSessionBuilder.New()
            .WithProject(ProjectId).WithFacilitator(UserId).Build();
        session.Votes = [];
        _pokerRepo.GetByIdWithVotesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new ConfirmPokerEstimateHandler(
            _pokerRepo, _workItemRepo, _historyService, _currentUser, _permissions, _publisher);
        var result = await handler.Handle(new ConfirmPokerEstimateCommand(session.Id, 8), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("revealed");
    }

    // --- GetPokerSession ---

    [Fact]
    public async Task Get_VotesHiddenBeforeReveal()
    {
        var session = PlanningPokerSessionBuilder.New().WithProject(ProjectId).Build();
        session.Votes =
        [
            new PlanningPokerVote { VoterId = Guid.NewGuid(), Value = 5, Voter = new User { Name = "Dev1" } }
        ];
        _pokerRepo.GetByIdWithVotesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new GetPokerSessionHandler(_pokerRepo, _currentUser, _permissions);
        var result = await handler.Handle(new GetPokerSessionQuery(session.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Votes.Should().HaveCount(1);
        result.Value.Votes[0].Value.Should().BeNull();
    }

    [Fact]
    public async Task Get_VotesVisibleAfterReveal()
    {
        var session = PlanningPokerSessionBuilder.New().WithProject(ProjectId).Revealed().Build();
        session.Votes =
        [
            new PlanningPokerVote { VoterId = Guid.NewGuid(), Value = 5, Voter = new User { Name = "Dev1" } }
        ];
        _pokerRepo.GetByIdWithVotesAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var handler = new GetPokerSessionHandler(_pokerRepo, _currentUser, _permissions);
        var result = await handler.Handle(new GetPokerSessionQuery(session.Id, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Votes[0].Value.Should().Be(5);
    }
}
