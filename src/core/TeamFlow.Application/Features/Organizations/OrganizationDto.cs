using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Organizations;

public sealed record OrganizationDto(
    Guid Id,
    string Name,
    string Slug,
    DateTime CreatedAt
);

public sealed record MyOrganizationDto(
    Guid Id,
    string Name,
    string Slug,
    OrgRole Role,
    DateTime JoinedAt
);
