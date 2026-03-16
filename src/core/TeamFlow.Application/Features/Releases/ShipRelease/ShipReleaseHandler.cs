using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Errors;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Releases.ShipRelease;

public sealed class ShipReleaseHandler(
    IReleaseRepository releaseRepo,
    IWorkItemRepository workItemRepo,
    ICurrentUser currentUser,
    IPermissionChecker permissions)
    : IRequestHandler<ShipReleaseCommand, Result<ShipReleaseResultDto>>
{
    public async Task<Result<ShipReleaseResultDto>> Handle(ShipReleaseCommand request, CancellationToken ct)
    {
        var release = await releaseRepo.GetByIdAsync(request.ReleaseId, ct);
        if (release is null)
            return DomainError.NotFound<ShipReleaseResultDto>("Release not found");

        if (!await permissions.HasPermissionAsync(currentUser.Id, release.ProjectId, Permission.Release_Publish, ct))
            return DomainError.Forbidden<ShipReleaseResultDto>();

        if (release.Status == ReleaseStatus.Released)
            return DomainError.Conflict<ShipReleaseResultDto>("Release has already been shipped");

        var items = await workItemRepo.GetByReleaseIdAsync(request.ReleaseId, ct);

        var openItems = items
            .Where(i => i.Status != WorkItemStatus.Done && i.Status != WorkItemStatus.Rejected)
            .ToList();

        if (openItems.Count > 0 && !request.ConfirmOpenItems)
        {
            var incompleteItems = openItems
                .Select(i => new IncompleteItemDto(i.Id, i.Title, i.Status))
                .ToList();

            return Result.Success(new ShipReleaseResultDto(false, incompleteItems));
        }

        release.Status = ReleaseStatus.Released;
        release.ReleasedAt = DateTime.UtcNow;
        release.ReleasedById = currentUser.Id;
        release.NotesLocked = true;
        await releaseRepo.UpdateAsync(release, ct);

        return Result.Success(new ShipReleaseResultDto(true, null));
    }
}
