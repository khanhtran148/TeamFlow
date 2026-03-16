namespace TeamFlow.Application.Features.Users;

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string Name,
    string? AvatarUrl,
    string SystemRole,
    DateTime CreatedAt,
    IReadOnlyList<ProfileOrganizationDto> Organizations,
    IReadOnlyList<ProfileTeamDto> Teams
);

public sealed record ProfileOrganizationDto(
    Guid OrgId,
    string OrgName,
    string OrgSlug,
    string Role,
    DateTime JoinedAt
);

public sealed record ProfileTeamDto(
    Guid TeamId,
    string TeamName,
    Guid OrgId,
    string OrgName,
    string Role,
    DateTime JoinedAt
);
