using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.OrgMembers.ChangeRole;

public sealed record ChangeOrgMemberRoleCommand(
    Guid OrgId,
    Guid UserId,
    OrgRole NewRole
) : IRequest<Result>;
