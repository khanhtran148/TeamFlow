using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Search.DeleteSavedFilter;

public sealed record DeleteSavedFilterCommand(
    Guid ProjectId,
    Guid FilterId
) : IRequest<Result>;
