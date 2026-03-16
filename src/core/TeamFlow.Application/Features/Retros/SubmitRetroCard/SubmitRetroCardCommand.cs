using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Domain.Enums;

namespace TeamFlow.Application.Features.Retros.SubmitRetroCard;

public sealed record SubmitRetroCardCommand(
    Guid SessionId,
    RetroCardCategory Category,
    string Content
) : IRequest<Result<RetroCardDto>>;
