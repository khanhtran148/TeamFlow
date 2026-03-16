using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.PlanningPoker.CreatePokerSession;

public sealed class CreatePokerSessionHandler(
    IPlanningPokerSessionRepository pokerRepo,
    IWorkItemRepository workItemRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<CreatePokerSessionCommand, Result<PokerSessionDto>>
{
    public async Task<Result<PokerSessionDto>> Handle(CreatePokerSessionCommand request, CancellationToken ct)
    {
        var workItem = await workItemRepo.GetByIdAsync(request.WorkItemId, ct);
        if (workItem is null)
            return DomainError.NotFound<PokerSessionDto>("Work item not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, workItem.ProjectId, Permission.Poker_Facilitate, ct))
            return DomainError.Forbidden<PokerSessionDto>();

        var existing = await pokerRepo.GetActiveByWorkItemAsync(request.WorkItemId, ct);
        if (existing is not null)
            return DomainError.Conflict<PokerSessionDto>("An active poker session already exists for this work item");

        var session = new PlanningPokerSession
        {
            WorkItemId = request.WorkItemId,
            ProjectId = workItem.ProjectId,
            FacilitatorId = currentUser.Id
        };

        await pokerRepo.AddAsync(session, ct);

        await publisher.Publish(new PokerSessionCreatedDomainEvent(
            session.Id, session.WorkItemId, session.ProjectId, session.FacilitatorId), ct);

        return Result.Success(PokerMapper.ToDto(session));
    }
}
