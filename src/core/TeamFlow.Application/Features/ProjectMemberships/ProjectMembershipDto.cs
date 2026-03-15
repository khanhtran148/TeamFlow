using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.ProjectMemberships;

public sealed record ProjectMembershipDto(
    Guid Id,
    Guid ProjectId,
    Guid MemberId,
    string MemberType,
    string MemberName,
    ProjectRole Role,
    DateTime CreatedAt
);
