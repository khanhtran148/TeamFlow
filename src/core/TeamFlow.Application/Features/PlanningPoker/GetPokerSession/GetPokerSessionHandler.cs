using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.PlanningPoker.GetPokerSession;

public sealed class GetPokerSessionHandler(
    IPlanningPokerSessionRepository pokerRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<GetPokerSessionQuery, Result<PokerSessionDto>>
{
    public async Task<Result<PokerSessionDto>> Handle(GetPokerSessionQuery request, CancellationToken ct)
    {
        Domain.Entities.PlanningPokerSession? session;

        if (request.SessionId.HasValue)
            session = await pokerRepo.GetByIdWithVotesAsync(request.SessionId.Value, ct);
        else if (request.WorkItemId.HasValue)
            session = await pokerRepo.GetActiveByWorkItemAsync(request.WorkItemId.Value, ct);
        else
            return DomainError.Validation<PokerSessionDto>("Either SessionId or WorkItemId must be provided");

        if (session is null)
            return DomainError.NotFound<PokerSessionDto>("Poker session not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, session.ProjectId, Permission.Poker_View, ct))
            return DomainError.Forbidden<PokerSessionDto>();

        return Result.Success(PokerMapper.ToDto(session));
    }
}
