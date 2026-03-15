namespace TeamFlow.Application.Features.Projects;

public sealed record ProjectDto(
    Guid Id,
    Guid OrgId,
    string Name,
    string? Description,
    string Status,
    int EpicCount,
    int OpenItemCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
