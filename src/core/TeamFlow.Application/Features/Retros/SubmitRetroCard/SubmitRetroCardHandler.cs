using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Retros.SubmitRetroCard;

public sealed class SubmitRetroCardHandler(
    IRetroSessionRepository retroRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<SubmitRetroCardCommand, Result<RetroCardDto>>
{
    public async Task<Result<RetroCardDto>> Handle(SubmitRetroCardCommand request, CancellationToken ct)
    {
        var session = await retroRepo.GetByIdAsync(request.SessionId, ct);
        if (session is null)
            return DomainError.NotFound<RetroCardDto>("Retro session not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, session.ProjectId, Permission.Retro_SubmitCard, ct))
            return DomainError.Forbidden<RetroCardDto>();

        if (session.Status != RetroSessionStatus.Open)
            return DomainError.Validation<RetroCardDto>("Cards can only be submitted when session is Open");

        var card = new RetroCard
        {
            SessionId = request.SessionId,
            AuthorId = currentUser.Id,
            Category = request.Category,
            Content = request.Content
        };

        await retroRepo.AddCardAsync(card, ct);

        await publisher.Publish(new RetroCardSubmittedDomainEvent(
            session.Id, card.Id, currentUser.Id), ct);

        var isAnonymous = session.AnonymityMode == RetroAnonymityModes.Anonymous;
        return Result.Success(RetroMapper.ToCardDto(card, isAnonymous));
    }
}
