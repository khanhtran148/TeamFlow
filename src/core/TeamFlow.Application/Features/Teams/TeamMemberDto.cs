using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Teams;

public sealed record TeamMemberDto(
    Guid Id,
    Guid UserId,
    string UserName,
    string UserEmail,
    ProjectRole Role,
    DateTime JoinedAt
);
