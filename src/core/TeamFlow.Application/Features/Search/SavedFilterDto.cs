namespace TeamFlow.Application.Features.Search;

public sealed record SavedFilterDto(
    Guid Id,
    string Name,
    object FilterJson,
    bool IsDefault,
    DateTime CreatedAt
);
