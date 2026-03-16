using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Retros.MarkCardDiscussed;

public sealed class MarkCardDiscussedHandler(
    IRetroSessionRepository retroRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<MarkCardDiscussedCommand, Result>
{
    public async Task<Result> Handle(MarkCardDiscussedCommand request, CancellationToken ct)
    {
        var card = await retroRepo.GetCardByIdAsync(request.CardId, ct);
        if (card is null)
            return DomainError.NotFound("Card not found");

        var session = card.Session;
        if (session is null)
            return DomainError.NotFound("Retro session not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, session.ProjectId, Permission.Retro_Facilitate, ct))
            return DomainError.Forbidden();

        if (session.FacilitatorId != currentUser.Id)
            return DomainError.Forbidden("Only the facilitator can mark cards as discussed");

        if (session.Status != RetroSessionStatus.Discussing)
            return DomainError.Validation("Cards can only be marked as discussed during Discussing phase");

        card.IsDiscussed = true;
        await retroRepo.UpdateCardAsync(card, ct);

        return Result.Success();
    }
}
