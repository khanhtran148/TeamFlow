using System.Text.Json;
using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Search.SaveFilter;

public sealed record SaveFilterCommand(
    Guid ProjectId,
    string Name,
    JsonDocument FilterJson,
    bool IsDefault
) : IRequest<Result<SavedFilterDto>>;
