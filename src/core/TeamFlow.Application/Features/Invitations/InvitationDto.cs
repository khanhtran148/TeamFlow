using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Invitations;

/// <summary>Standard invitation DTO — no raw token.</summary>
public sealed record InvitationDto(
    Guid Id,
    Guid OrganizationId,
    string? Email,
    OrgRole Role,
    InviteStatus Status,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    string? AcceptedByUserName
);

/// <summary>Response returned only on creation — includes the raw token (shown once). Caller constructs URL.</summary>
public sealed record CreateInvitationResponse(
    Guid Id,
    string Token,
    OrgRole Role,
    DateTime ExpiresAt,
    InviteStatus Status
);

/// <summary>Response returned when accepting an invitation — org info for redirect.</summary>
public sealed record AcceptInvitationResponse(
    Guid OrganizationId,
    string OrganizationSlug,
    OrgRole Role
);
