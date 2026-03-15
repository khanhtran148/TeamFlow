using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Releases.DeleteRelease;

public sealed class DeleteReleaseHandler(
    IReleaseRepository releaseRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<DeleteReleaseCommand, Result>
{
    public async Task<Result> Handle(DeleteReleaseCommand request, CancellationToken ct)
    {
        var release = await releaseRepository.GetByIdAsync(request.ReleaseId, ct);
        if (release is null)
            return Result.Failure("Release not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, release.ProjectId, Permission.Release_Edit, ct))
            return Result.Failure("Access denied");

        if (release.Status == ReleaseStatus.Released)
            return Result.Failure("Cannot delete a released release");

        // Unlink all work items first
        await releaseRepository.UnlinkAllItemsAsync(request.ReleaseId, ct);

        await releaseRepository.DeleteAsync(request.ReleaseId, ct);

        return Result.Success();
    }
}
