using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Retros.CreateRetroActionItem;

public sealed record CreateRetroActionItemCommand(
    Guid SessionId,
    Guid? CardId,
    string Title,
    string? Description,
    Guid? AssigneeId,
    DateOnly? DueDate,
    bool LinkToBacklog = false
) : IRequest<Result<RetroActionItemDto>>;
