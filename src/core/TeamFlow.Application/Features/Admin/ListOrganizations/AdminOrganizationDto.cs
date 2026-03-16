namespace TeamFlow.Application.Features.Admin.ListOrganizations;

public sealed record AdminOrganizationDto(
    Guid Id,
    string Name,
    string Slug,
    int MemberCount,
    DateTime CreatedAt,
    bool IsActive
);
