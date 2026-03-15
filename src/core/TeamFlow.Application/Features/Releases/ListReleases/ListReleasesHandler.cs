using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Releases;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Releases.ListReleases;

public sealed class ListReleasesHandler(IReleaseRepository releaseRepository)
    : IRequestHandler<ListReleasesQuery, Result<PagedResult<ReleaseDto>>>
{
    public async Task<Result<PagedResult<ReleaseDto>>> Handle(ListReleasesQuery request, CancellationToken ct)
    {
        var (items, totalCount) = await releaseRepository.ListByProjectAsync(
            request.ProjectId, request.Page, request.PageSize, ct);

        var dtos = items.Select(MapToDto);

        return Result.Success(new PagedResult<ReleaseDto>(dtos, totalCount, request.Page, request.PageSize));
    }

    private static ReleaseDto MapToDto(Release release) =>
        new(release.Id, release.ProjectId, release.Name, release.Description,
            release.ReleaseDate, release.Status, release.NotesLocked,
            release.WorkItems.Count, [], release.CreatedAt);
}
