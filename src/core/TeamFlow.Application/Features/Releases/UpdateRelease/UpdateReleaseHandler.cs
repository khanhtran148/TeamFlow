using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases;
using TeamFlow.Domain.Entities;

namespace TeamFlow.Application.Features.Releases.UpdateRelease;

public sealed class UpdateReleaseHandler(
    IReleaseRepository releaseRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<UpdateReleaseCommand, Result<ReleaseDto>>
{
    public async Task<Result<ReleaseDto>> Handle(UpdateReleaseCommand request, CancellationToken ct)
    {
        var release = await releaseRepository.GetByIdAsync(request.ReleaseId, ct);
        if (release is null)
            return Result.Failure<ReleaseDto>("Release not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, release.ProjectId, Permission.Release_Edit, ct))
            return Result.Failure<ReleaseDto>("Access denied");

        if (release.NotesLocked)
            return Result.Failure<ReleaseDto>("Release notes are locked and cannot be updated");

        release.Name = request.Name;
        release.Description = request.Description;
        release.ReleaseDate = request.ReleaseDate;

        await releaseRepository.UpdateAsync(release, ct);

        return Result.Success(MapToDto(release));
    }

    private static ReleaseDto MapToDto(Release release) =>
        new(release.Id, release.ProjectId, release.Name, release.Description,
            release.ReleaseDate, release.Status, release.NotesLocked, 0, [], release.CreatedAt);
}
