using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.ProjectMemberships.RemoveProjectMember;

public sealed record RemoveProjectMemberCommand(Guid MembershipId) : IRequest<Result>;
