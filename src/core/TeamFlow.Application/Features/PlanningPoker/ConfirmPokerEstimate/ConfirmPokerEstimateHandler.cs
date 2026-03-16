using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.PlanningPoker.ConfirmPokerEstimate;

public sealed class ConfirmPokerEstimateHandler(
    IPlanningPokerSessionRepository pokerRepo,
    IWorkItemRepository workItemRepo,
    IHistoryService historyService,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<ConfirmPokerEstimateCommand, Result<PokerSessionDto>>
{
    public async Task<Result<PokerSessionDto>> Handle(ConfirmPokerEstimateCommand request, CancellationToken ct)
    {
        var session = await pokerRepo.GetByIdWithVotesAsync(request.SessionId, ct);
        if (session is null)
            return DomainError.NotFound<PokerSessionDto>("Poker session not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, session.ProjectId, Permission.Poker_ConfirmEstimate, ct))
            return DomainError.Forbidden<PokerSessionDto>();

        if (session.FacilitatorId != currentUser.Id)
            return DomainError.Forbidden<PokerSessionDto>("Only the facilitator can confirm the estimate");

        if (!session.IsRevealed)
            return DomainError.Validation<PokerSessionDto>("Votes must be revealed before confirming an estimate");

        session.FinalEstimate = request.FinalEstimate;
        session.ConfirmedById = currentUser.Id;
        session.ClosedAt = DateTime.UtcNow;
        await pokerRepo.UpdateAsync(session, ct);

        // Update work item estimation
        var workItem = await workItemRepo.GetByIdAsync(session.WorkItemId, ct);
        if (workItem is not null)
        {
            var oldValue = workItem.EstimationValue?.ToString();
            workItem.EstimationValue = request.FinalEstimate;
            workItem.EstimationSource = "Poker";
            await workItemRepo.UpdateAsync(workItem, ct);

            await historyService.RecordAsync(new WorkItemHistoryEntry(
                workItem.Id,
                currentUser.Id,
                "FieldChanged",
                "EstimationValue",
                oldValue,
                request.FinalEstimate.ToString()
            ), ct);
        }

        await publisher.Publish(new PokerEstimateConfirmedDomainEvent(
            session.Id, session.WorkItemId, session.ProjectId, request.FinalEstimate, currentUser.Id), ct);

        return Result.Success(PokerMapper.ToDto(session));
    }
}
