using System.Text.Json;
using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Retros.CloseRetroSession;

public sealed class CloseRetroSessionHandler(
    IRetroSessionRepository retroRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<CloseRetroSessionCommand, Result<RetroSessionDto>>
{
    public async Task<Result<RetroSessionDto>> Handle(CloseRetroSessionCommand request, CancellationToken ct)
    {
        var session = await retroRepo.GetByIdWithDetailsAsync(request.SessionId, ct);
        if (session is null)
            return DomainError.NotFound<RetroSessionDto>("Retro session not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, session.ProjectId, Permission.Retro_Facilitate, ct))
            return DomainError.Forbidden<RetroSessionDto>();

        if (session.FacilitatorId != currentUser.Id)
            return DomainError.Forbidden<RetroSessionDto>("Only the facilitator can close the session");

        if (session.Status != RetroSessionStatus.Discussing)
            return DomainError.Validation<RetroSessionDto>("Session can only be closed from Discussing status");

        session.Status = RetroSessionStatus.Closed;

        // Generate summary
        var summary = new
        {
            CardCountByCategory = session.Cards
                .GroupBy(c => c.Category)
                .ToDictionary(g => g.Key.ToString(), g => g.Count()),
            TopVotedCards = session.Cards
                .OrderByDescending(c => c.Votes.Sum(v => v.VoteCount))
                .Take(5)
                .Select(c => new { c.Id, c.Content, c.Category, Votes = c.Votes.Sum(v => v.VoteCount) }),
            ActionItemCount = session.ActionItems.Count
        };

        session.AiSummary = JsonDocument.Parse(JsonSerializer.Serialize(summary));
        await retroRepo.UpdateAsync(session, ct);

        await publisher.Publish(new RetroSessionClosedDomainEvent(
            session.Id, session.ProjectId, session.SprintId,
            session.Cards.Count, session.ActionItems.Count, session.FacilitatorId), ct);

        return Result.Success(RetroMapper.ToDto(session, session.AnonymityMode == RetroAnonymityModes.Anonymous));
    }
}
