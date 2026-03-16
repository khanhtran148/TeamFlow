using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Retros.CreateRetroSession;

public sealed record CreateRetroSessionCommand(
    Guid ProjectId,
    string? Name,
    Guid? SprintId,
    string AnonymityMode = "Public"
) : IRequest<Result<RetroSessionDto>>;
