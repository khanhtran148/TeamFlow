using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;

namespace TeamFlow.Application.Features.Releases.UpdateReleaseNotes;

public sealed class UpdateReleaseNotesHandler(
    IReleaseRepository releaseRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<UpdateReleaseNotesCommand, Result>
{
    public async Task<Result> Handle(UpdateReleaseNotesCommand request, CancellationToken ct)
    {
        var release = await releaseRepo.GetByIdAsync(request.ReleaseId, ct);
        if (release is null)
            return DomainError.NotFound("Release not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, release.ProjectId, Permission.Release_Edit, ct))
            return DomainError.Forbidden();

        if (release.NotesLocked)
            return DomainError.Validation("Release notes are locked after shipping");

        release.ReleaseNotes = request.Notes;
        await releaseRepo.UpdateAsync(release, ct);

        return Result.Success();
    }
}
