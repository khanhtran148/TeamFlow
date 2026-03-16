using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Retros.RenameRetroSession;

public sealed record RenameRetroSessionCommand(
    Guid SessionId,
    string Name
) : IRequest<Result>;
