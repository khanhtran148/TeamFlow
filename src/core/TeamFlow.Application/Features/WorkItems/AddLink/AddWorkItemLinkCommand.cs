using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.WorkItems.AddLink;

public sealed record AddWorkItemLinkCommand(
    Guid SourceId,
    Guid TargetId,
    LinkType LinkType
) : IRequest<Result>;
