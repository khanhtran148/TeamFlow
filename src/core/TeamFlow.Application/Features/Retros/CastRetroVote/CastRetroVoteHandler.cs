using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Retros.CastRetroVote;

public sealed class CastRetroVoteHandler(
    IRetroSessionRepository retroRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<CastRetroVoteCommand, Result>
{
    private const int MaxVotesPerSession = 5;
    private const short MaxVotesPerCard = 2;

    public async Task<Result> Handle(CastRetroVoteCommand request, CancellationToken ct)
    {
        if (request.VoteCount is < 1 or > MaxVotesPerCard)
            return DomainError.Validation($"VoteCount must be between 1 and {MaxVotesPerCard}");

        var card = await retroRepo.GetCardByIdAsync(request.CardId, ct);
        if (card is null)
            return DomainError.NotFound("Card not found");

        var session = card.Session;
        if (session is null)
            return DomainError.NotFound("Retro session not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, session.ProjectId, Permission.Retro_Vote, ct))
            return DomainError.Forbidden();

        if (session.Status != RetroSessionStatus.Voting)
            return DomainError.Validation("Votes can only be cast when session is in Voting status");

        var existingVote = await retroRepo.GetVoteAsync(request.CardId, currentUser.Id, ct);
        if (existingVote is not null)
            return DomainError.Conflict("You have already voted on this card");

        var totalVotes = await retroRepo.GetTotalVoteCountForUserInSessionAsync(
            session.Id, currentUser.Id, ct);

        if (totalVotes + request.VoteCount > MaxVotesPerSession)
            return DomainError.Validation($"You have exceeded the maximum of {MaxVotesPerSession} votes per session");

        var vote = new RetroVote
        {
            CardId = request.CardId,
            VoterId = currentUser.Id,
            VoteCount = request.VoteCount
        };

        await retroRepo.AddVoteAsync(vote, ct);

        await publisher.Publish(new RetroVoteCastDomainEvent(
            session.Id, card.Id, currentUser.Id, request.VoteCount), ct);

        return Result.Success();
    }
}
