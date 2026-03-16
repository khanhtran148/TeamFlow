using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.OrgMembers.List;

public sealed record ListOrgMembersQuery(Guid OrgId) : IRequest<Result<IReadOnlyList<OrgMemberDto>>>;
