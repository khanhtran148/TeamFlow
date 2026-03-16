namespace TeamFlow.Application.Features.Users;

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string Name,
    IReadOnlyList<UserOrganizationDto> Organizations
);

public sealed record UserOrganizationDto(
    Guid OrgId,
    string OrgName
);
