using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Releases.DeleteRelease;

public sealed record DeleteReleaseCommand(Guid ReleaseId) : IRequest<Result>;
