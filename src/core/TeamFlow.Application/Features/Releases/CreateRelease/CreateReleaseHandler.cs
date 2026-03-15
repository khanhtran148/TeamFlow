using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Interfaces;
using TeamFlow.Application.Features.Releases;
using TeamFlow.Domain.Entities;
using TeamFlow.Domain.Events;

namespace TeamFlow.Application.Features.Releases.CreateRelease;

public sealed class CreateReleaseHandler(
    IReleaseRepository releaseRepository,
    IProjectRepository projectRepository,
    ICurrentUser currentUser,
    IPermissionChecker permissions,
    IPublisher publisher)
    : IRequestHandler<CreateReleaseCommand, Result<ReleaseDto>>
{
    public async Task<Result<ReleaseDto>> Handle(CreateReleaseCommand request, CancellationToken ct)
    {
        if (!await permissions.HasPermissionAsync(currentUser.Id, request.ProjectId, Permission.Release_Create, ct))
            return Result.Failure<ReleaseDto>("Access denied");

        if (!await projectRepository.ExistsAsync(request.ProjectId, ct))
            return Result.Failure<ReleaseDto>("Project not found");

        var release = new Release
        {
            ProjectId = request.ProjectId,
            Name = request.Name,
            Description = request.Description,
            ReleaseDate = request.ReleaseDate
        };

        await releaseRepository.AddAsync(release, ct);

        await publisher.Publish(new ReleaseCreatedDomainEvent(
            release.Id, release.ProjectId, release.Name, currentUser.Id), ct);

        return Result.Success(MapToDto(release));
    }

    private static ReleaseDto MapToDto(Release release) =>
        new(
            release.Id,
            release.ProjectId,
            release.Name,
            release.Description,
            release.ReleaseDate,
            release.Status,
            release.NotesLocked,
            0,
            [],
            release.CreatedAt
        );
}
