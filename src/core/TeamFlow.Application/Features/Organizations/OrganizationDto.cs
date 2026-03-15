namespace TeamFlow.Application.Features.Organizations;

public sealed record OrganizationDto(
    Guid Id,
    string Name,
    DateTime CreatedAt
);
