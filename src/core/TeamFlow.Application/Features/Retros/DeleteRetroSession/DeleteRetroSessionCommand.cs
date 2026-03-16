using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Retros.DeleteRetroSession;

public sealed record DeleteRetroSessionCommand(Guid SessionId) : IRequest<Result>;
