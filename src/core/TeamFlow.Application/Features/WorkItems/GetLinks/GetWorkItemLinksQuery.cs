using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.WorkItems.GetLinks;

public sealed record GetWorkItemLinksQuery(Guid WorkItemId) : IRequest<Result<WorkItemLinksDto>>;

public sealed record WorkItemLinksDto(
    Guid WorkItemId,
    IEnumerable<LinkGroupDto> Groups
);

public sealed record LinkGroupDto(
    LinkType LinkType,
    IEnumerable<LinkedItemDto> Items
);

public sealed record LinkedItemDto(
    Guid Id,
    string Title,
    WorkItemType Type,
    WorkItemStatus Status,
    LinkScope Scope
);
