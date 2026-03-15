using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.ProjectMemberships;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.ProjectMemberships.AddProjectMember;

public sealed record AddProjectMemberCommand(
    Guid ProjectId,
    Guid MemberId,
    string MemberType,
    ProjectRole Role
) : IRequest<Result<ProjectMembershipDto>>;
