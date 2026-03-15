using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.ProjectMemberships;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.ProjectMemberships.ListProjectMemberships;

public sealed class ListProjectMembershipsHandler(IProjectMembershipRepository membershipRepository)
    : IRequestHandler<ListProjectMembershipsQuery, Result<IEnumerable<ProjectMembershipDto>>>
{
    public async Task<Result<IEnumerable<ProjectMembershipDto>>> Handle(
        ListProjectMembershipsQuery request, CancellationToken ct)
    {
        var memberships = await membershipRepository.GetByProjectAsync(request.ProjectId, ct);

        var dtos = memberships.Select(m => new ProjectMembershipDto(
            m.Id,
            m.ProjectId,
            m.MemberId,
            m.MemberType,
            "Unknown",
            m.Role,
            m.CreatedAt
        ));

        return Result.Success(dtos);
    }
}
