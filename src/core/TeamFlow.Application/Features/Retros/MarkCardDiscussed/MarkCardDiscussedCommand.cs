using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Retros.MarkCardDiscussed;

public sealed record MarkCardDiscussedCommand(Guid CardId) : IRequest<Result>;
