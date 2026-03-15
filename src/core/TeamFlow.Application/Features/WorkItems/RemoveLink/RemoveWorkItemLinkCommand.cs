using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.WorkItems.RemoveLink;

public sealed record RemoveWorkItemLinkCommand(Guid LinkId) : IRequest<Result>;
