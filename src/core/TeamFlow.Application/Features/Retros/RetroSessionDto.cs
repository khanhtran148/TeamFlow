using System.Text.Json;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Retros;

public sealed record RetroSessionDto(
    Guid Id,
    string Name,
    Guid ProjectId,
    Guid? SprintId,
    Guid FacilitatorId,
    string FacilitatorName,
    string AnonymityMode,
    RetroSessionStatus Status,
    JsonDocument? AiSummary,
    JsonDocument? ColumnsConfig,
    IReadOnlyList<RetroCardDto> Cards,
    IReadOnlyList<RetroActionItemDto> ActionItems,
    DateTime CreatedAt
);

public sealed record RetroCardDto(
    Guid Id,
    Guid? AuthorId,
    string? AuthorName,
    RetroCardCategory Category,
    string Content,
    bool IsDiscussed,
    int TotalVotes,
    DateTime CreatedAt
);

public sealed record RetroActionItemDto(
    Guid Id,
    Guid? CardId,
    string Title,
    string? Description,
    Guid? AssigneeId,
    string? AssigneeName,
    DateOnly? DueDate,
    Guid? LinkedTaskId,
    DateTime CreatedAt
);

public sealed record RetroSessionSummaryDto(
    Guid Id,
    string Name,
    Guid ProjectId,
    Guid? SprintId,
    string FacilitatorName,
    string AnonymityMode,
    RetroSessionStatus Status,
    int CardCount,
    int ActionItemCount,
    DateTime CreatedAt
);
