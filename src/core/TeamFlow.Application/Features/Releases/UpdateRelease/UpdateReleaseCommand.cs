using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Releases;

namespace TeamFlow.Application.Features.Releases.UpdateRelease;

public sealed record UpdateReleaseCommand(
    Guid ReleaseId,
    string Name,
    string? Description,
    DateOnly? ReleaseDate
) : IRequest<Result<ReleaseDto>>;
