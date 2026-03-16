using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Organizations.ListMyOrganizations;

public sealed class ListMyOrganizationsHandler(
    IOrganizationMemberRepository memberRepository,
    ICurrentUser currentUser)
    : IRequestHandler<ListMyOrganizationsQuery, Result<IEnumerable<MyOrganizationDto>>>
{
    public async Task<Result<IEnumerable<MyOrganizationDto>>> Handle(
        ListMyOrganizationsQuery request, CancellationToken ct)
    {
        var memberships = await memberRepository.ListOrganizationsForUserAsync(currentUser.Id, ct);

        var dtos = memberships.Select(m => new MyOrganizationDto(
            m.Org.Id,
            m.Org.Name,
            m.Org.Slug,
            m.Role,
            m.JoinedAt));

        return Result.Success(dtos);
    }
}
