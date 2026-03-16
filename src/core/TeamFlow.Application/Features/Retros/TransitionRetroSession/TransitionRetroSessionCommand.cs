using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Retros.TransitionRetroSession;

public sealed record TransitionRetroSessionCommand(
    Guid SessionId,
    RetroSessionStatus TargetStatus
) : IRequest<Result<RetroSessionDto>>;
