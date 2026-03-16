using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Retros.GetPreviousActionItems;

public sealed record GetPreviousActionItemsQuery(Guid ProjectId)
    : IRequest<Result<IReadOnlyList<RetroActionItemDto>>>;
