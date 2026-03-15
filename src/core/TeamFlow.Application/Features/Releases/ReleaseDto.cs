using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Releases;

public sealed record ReleaseDto(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Description,
    DateOnly? ReleaseDate,
    ReleaseStatus Status,
    bool NotesLocked,
    int TotalItems,
    Dictionary<string, int> ItemCountsByStatus,
    DateTime CreatedAt
);
