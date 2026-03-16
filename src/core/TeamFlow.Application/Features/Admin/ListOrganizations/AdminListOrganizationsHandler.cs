using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Admin.ListOrganizations;

public sealed class AdminListOrganizationsHandler(
    IOrganizationRepository organizationRepository,
    ICurrentUser currentUser)
    : IRequestHandler<AdminListOrganizationsQuery, Result<PagedResult<AdminOrganizationDto>>>
{
    public async Task<Result<PagedResult<AdminOrganizationDto>>> Handle(
        AdminListOrganizationsQuery request, CancellationToken ct)
    {
        if (currentUser.SystemRole != SystemRole.SystemAdmin)
            return DomainError.Forbidden<PagedResult<AdminOrganizationDto>>("Access forbidden.");

        var (orgs, totalCount) = await organizationRepository.ListAllPagedAsync(
            request.Search, request.Page, request.PageSize, ct);

        var dtos = orgs.Select(o => new AdminOrganizationDto(
            o.Id,
            o.Name,
            o.Slug,
            o.Members.Count,
            o.CreatedAt,
            o.IsActive));

        return Result.Success(new PagedResult<AdminOrganizationDto>(dtos, totalCount, request.Page, request.PageSize));
    }
}
