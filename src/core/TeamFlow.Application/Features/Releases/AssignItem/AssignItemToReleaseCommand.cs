using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Releases.AssignItem;

public sealed record AssignItemToReleaseCommand(
    Guid ReleaseId,
    Guid WorkItemId
) : IRequest<Result>;
