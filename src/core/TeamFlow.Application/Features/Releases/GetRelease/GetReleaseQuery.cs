using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Features.Releases;

namespace TeamFlow.Application.Features.Releases.GetRelease;

public sealed record GetReleaseQuery(Guid ReleaseId) : IRequest<Result<ReleaseDto>>;
