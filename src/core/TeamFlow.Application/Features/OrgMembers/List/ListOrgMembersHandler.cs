using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.OrgMembers.List;

public sealed class ListOrgMembersHandler(
    IOrganizationMemberRepository memberRepository,
    ICurrentUser currentUser)
    : IRequestHandler<ListOrgMembersQuery, Result<IReadOnlyList<OrgMemberDto>>>
{
    public async Task<Result<IReadOnlyList<OrgMemberDto>>> Handle(
        ListOrgMembersQuery request, CancellationToken ct)
    {
        // 1. Permission check — must be an org member to view the list
        var isMember = await memberRepository.IsMemberAsync(request.OrgId, currentUser.Id, ct);
        if (!isMember)
            return DomainError.Forbidden<IReadOnlyList<OrgMemberDto>>(
                "You must be a member of this organization to view its members.");

        // 2. Load members with user info
        var members = await memberRepository.ListByOrgWithUsersAsync(request.OrgId, ct);

        // 3. Map to DTOs
        var dtos = members
            .Select(m => new OrgMemberDto(
                m.Member.UserId,
                m.User.Name,
                m.User.Email,
                m.Member.Role,
                m.Member.JoinedAt))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<OrgMemberDto>>(dtos);
    }
}
