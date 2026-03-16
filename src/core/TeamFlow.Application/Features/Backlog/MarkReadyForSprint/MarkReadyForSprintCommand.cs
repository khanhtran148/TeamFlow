using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Backlog.MarkReadyForSprint;

public sealed record MarkReadyForSprintCommand(
    Guid WorkItemId,
    bool IsReady
) : IRequest<Result>;
