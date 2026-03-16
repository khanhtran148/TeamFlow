using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.PlanningPoker.RevealPokerVotes;

public sealed class RevealPokerVotesHandler(
    IPlanningPokerSessionRepository pokerRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<RevealPokerVotesCommand, Result<PokerSessionDto>>
{
    public async Task<Result<PokerSessionDto>> Handle(RevealPokerVotesCommand request, CancellationToken ct)
    {
        var session = await pokerRepo.GetByIdWithVotesAsync(request.SessionId, ct);
        if (session is null)
            return DomainError.NotFound<PokerSessionDto>("Poker session not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, session.ProjectId, Permission.Poker_Facilitate, ct))
            return DomainError.Forbidden<PokerSessionDto>();

        if (session.FacilitatorId != currentUser.Id)
            return DomainError.Forbidden<PokerSessionDto>("Only the facilitator can reveal votes");

        if (session.IsRevealed)
            return DomainError.Conflict<PokerSessionDto>("Votes have already been revealed");

        session.IsRevealed = true;
        await pokerRepo.UpdateAsync(session, ct);

        await publisher.Publish(new PokerVotesRevealedDomainEvent(
            session.Id, session.ProjectId, session.FacilitatorId), ct);

        return Result.Success(PokerMapper.ToDto(session));
    }
}
