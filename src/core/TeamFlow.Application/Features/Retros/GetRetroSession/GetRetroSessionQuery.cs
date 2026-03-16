using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Retros.GetRetroSession;

public sealed record GetRetroSessionQuery(Guid SessionId) : IRequest<Result<RetroSessionDto>>;
