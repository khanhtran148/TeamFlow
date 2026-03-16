using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Retros.CloseRetroSession;

public sealed record CloseRetroSessionCommand(Guid SessionId) : IRequest<Result<RetroSessionDto>>;
