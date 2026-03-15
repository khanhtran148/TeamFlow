namespace TeamFlow.Application.Features.Teams;

public sealed record TeamDto(
    Guid Id,
    Guid OrgId,
    string Name,
    string? Description,
    int MemberCount,
    DateTime CreatedAt
);
