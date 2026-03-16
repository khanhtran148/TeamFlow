using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Retros;

internal static class RetroMapper
{
    public static RetroSessionDto ToDto(RetroSession session, bool isAnonymous)
    {
        var cards = session.Cards?.Select(c => ToCardDto(c, isAnonymous)).ToList()
            ?? [];
        var actionItems = session.ActionItems?.Select(ToActionItemDto).ToList()
            ?? [];

        return new RetroSessionDto(
            session.Id,
            session.ProjectId,
            session.SprintId,
            session.FacilitatorId,
            session.Facilitator?.Name ?? "Unknown",
            session.AnonymityMode,
            session.Status,
            session.AiSummary,
            cards,
            actionItems,
            session.CreatedAt
        );
    }

    public static RetroCardDto ToCardDto(RetroCard card, bool isAnonymous) =>
        new(
            card.Id,
            isAnonymous ? null : card.AuthorId,
            isAnonymous ? null : card.Author?.Name,
            card.Category,
            card.Content,
            card.IsDiscussed,
            card.Votes?.Sum(v => v.VoteCount) ?? 0,
            card.CreatedAt
        );

    public static RetroActionItemDto ToActionItemDto(RetroActionItem item) =>
        new(
            item.Id,
            item.CardId,
            item.Title,
            item.Description,
            item.AssigneeId,
            item.Assignee?.Name,
            item.DueDate,
            item.LinkedTaskId,
            item.CreatedAt
        );

    public static RetroSessionSummaryDto ToSummaryDto(RetroSession session) =>
        new(
            session.Id,
            session.ProjectId,
            session.SprintId,
            session.Facilitator?.Name ?? "Unknown",
            session.AnonymityMode,
            session.Status,
            session.Cards?.Count ?? 0,
            session.ActionItems?.Count ?? 0,
            session.CreatedAt
        );
}
