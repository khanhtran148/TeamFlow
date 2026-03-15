using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Releases.GetRelease;

public sealed class GetReleaseHandler(IReleaseRepository releaseRepository)
    : IRequestHandler<GetReleaseQuery, Result<ReleaseDto>>
{
    public async Task<Result<ReleaseDto>> Handle(GetReleaseQuery request, CancellationToken ct)
    {
        var release = await releaseRepository.GetByIdAsync(request.ReleaseId, ct);
        if (release is null)
            return Result.Failure<ReleaseDto>("Release not found");

        var statusCounts = await releaseRepository.GetItemStatusCountsAsync(request.ReleaseId, ct);
        var statusCountsStr = statusCounts.ToDictionary(
            kv => kv.Key.ToString(),
            kv => kv.Value);

        return Result.Success(new ReleaseDto(
            release.Id, release.ProjectId, release.Name, release.Description,
            release.ReleaseDate, release.Status, release.NotesLocked,
            release.WorkItems.Count, statusCountsStr, release.CreatedAt));
    }
}
