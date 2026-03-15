using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Releases;

namespace TeamFlow.Application.Features.Releases.CreateRelease;

public sealed record CreateReleaseCommand(
    Guid ProjectId,
    string Name,
    string? Description,
    DateOnly? ReleaseDate
) : IRequest<Result<ReleaseDto>>;
