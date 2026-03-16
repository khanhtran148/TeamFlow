namespace TeamFlow.Application.Features.Admin.ListOrganizations;

public sealed record AdminOrganizationDto(
    Guid Id,
    string Name,
    DateTime CreatedAt
);
