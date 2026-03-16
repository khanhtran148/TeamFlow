using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.OrgMembers.Remove;

public sealed record RemoveOrgMemberCommand(Guid OrgId, Guid UserId) : IRequest<Result>;
