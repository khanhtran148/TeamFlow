using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.ProjectMemberships;

namespace TeamFlow.Application.Features.ProjectMemberships.ListProjectMemberships;

public sealed class ListProjectMembershipsHandler(
    IProjectMembershipRepository membershipRepository,
    IUserRepository userRepository)
    : IRequestHandler<ListProjectMembershipsQuery, Result<IEnumerable<ProjectMembershipDto>>>
{
    public async Task<Result<IEnumerable<ProjectMembershipDto>>> Handle(
        ListProjectMembershipsQuery request, CancellationToken ct)
    {
        var memberships = await membershipRepository.GetByProjectAsync(request.ProjectId, ct);

        var userMemberIds = memberships
            .Where(m => m.MemberType == "User")
            .Select(m => m.MemberId)
            .Distinct()
            .ToList();

        var users = await userRepository.GetByIdsAsync(userMemberIds, ct);
        var userNameMap = users.ToDictionary(u => u.Id, u => u.Name);

        var dtos = memberships.Select(m => new ProjectMembershipDto(
            m.Id,
            m.ProjectId,
            m.MemberId,
            m.MemberType,
            userNameMap.GetValueOrDefault(m.MemberId, "Unknown"),
            m.Role,
            m.CreatedAt
        ));

        return Result.Success(dtos);
    }
}
