using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Retros.StartRetroSession;

public sealed record StartRetroSessionCommand(Guid SessionId) : IRequest<Result<RetroSessionDto>>;
