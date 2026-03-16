using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Invitations.List;

public sealed class ListInvitationsHandler(
    IInvitationRepository invitationRepository,
    IOrganizationMemberRepository memberRepository,
    ICurrentUser currentUser)
    : IRequestHandler<ListInvitationsQuery, Result<IEnumerable<InvitationDto>>>
{
    public async Task<Result<IEnumerable<InvitationDto>>> Handle(
        ListInvitationsQuery request, CancellationToken ct)
    {
        // Permission check — Owner or Admin only
        var role = await memberRepository.GetMemberRoleAsync(request.OrgId, currentUser.Id, ct);
        if (role is null || role == OrgRole.Member)
            return DomainError.Forbidden<IEnumerable<InvitationDto>>(
                "Only Org Owner or Admin can list invitations.");

        var invitations = await invitationRepository.ListByOrgAsync(request.OrgId, ct);

        var dtos = invitations.Select(i => new InvitationDto(
            i.Id,
            i.OrganizationId,
            i.Email,
            i.Role,
            i.Status,
            i.ExpiresAt,
            i.CreatedAt,
            i.AcceptedBy?.Name));

        return Result.Success<IEnumerable<InvitationDto>>(dtos);
    }
}
