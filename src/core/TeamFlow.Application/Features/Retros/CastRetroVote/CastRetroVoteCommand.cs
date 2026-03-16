using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Retros.CastRetroVote;

public sealed record CastRetroVoteCommand(
    Guid CardId,
    short VoteCount = 1
) : IRequest<Result>;
