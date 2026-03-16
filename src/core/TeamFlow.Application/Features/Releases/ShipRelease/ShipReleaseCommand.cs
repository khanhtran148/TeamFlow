using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Releases.ShipRelease;

public sealed record ShipReleaseCommand(
    Guid ReleaseId,
    bool ConfirmOpenItems = false
) : IRequest<Result<ShipReleaseResultDto>>;
