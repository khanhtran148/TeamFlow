using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Releases.UpdateReleaseNotes;

public sealed record UpdateReleaseNotesCommand(
    Guid ReleaseId,
    string Notes
) : IRequest<Result>;
