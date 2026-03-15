using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Releases.UnassignItem;

public sealed record UnassignItemFromReleaseCommand(
    Guid ReleaseId,
    Guid WorkItemId
) : IRequest<Result>;
