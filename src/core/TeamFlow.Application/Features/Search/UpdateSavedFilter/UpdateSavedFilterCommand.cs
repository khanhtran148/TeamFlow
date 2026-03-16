using System.Text.Json;
using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Search.UpdateSavedFilter;

public sealed record UpdateSavedFilterCommand(
    Guid ProjectId,
    Guid FilterId,
    string? Name,
    JsonDocument? FilterJson,
    bool? IsDefault
) : IRequest<Result<SavedFilterDto>>;
