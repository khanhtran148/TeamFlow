using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.PlanningPoker.CastPokerVote;

public sealed class CastPokerVoteHandler(
    IPlanningPokerSessionRepository pokerRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<CastPokerVoteCommand, Result>
{
    public async Task<Result> Handle(CastPokerVoteCommand request, CancellationToken ct)
    {
        var session = await pokerRepo.GetByIdAsync(request.SessionId, ct);
        if (session is null)
            return DomainError.NotFound("Poker session not found");

        if (session.ClosedAt is not null)
            return DomainError.Validation("Poker session is closed");

        if (!await permissions.HasPermissionAsync(currentUser.Id, session.ProjectId, Permission.Poker_Vote, ct))
            return DomainError.Forbidden();

        var existingVote = await pokerRepo.GetVoteAsync(request.SessionId, currentUser.Id, ct);
        if (existingVote is not null)
        {
            existingVote.Value = request.Value;
            existingVote.VotedAt = DateTime.UtcNow;
            await pokerRepo.UpdateVoteAsync(existingVote, ct);
        }
        else
        {
            var vote = new PlanningPokerVote
            {
                SessionId = request.SessionId,
                VoterId = currentUser.Id,
                Value = request.Value
            };
            await pokerRepo.AddVoteAsync(vote, ct);
        }

        var voteCount = await pokerRepo.GetVoteCountAsync(request.SessionId, ct);

        await publisher.Publish(new PokerVoteCastDomainEvent(
            session.Id, session.ProjectId, voteCount), ct);

        return Result.Success();
    }
}
