using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.OrgMembers;

public sealed record OrgMemberDto(
    Guid UserId,
    string UserName,
    string UserEmail,
    OrgRole Role,
    DateTime JoinedAt
);
