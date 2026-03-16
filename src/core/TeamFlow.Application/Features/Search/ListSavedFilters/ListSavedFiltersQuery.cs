using CSharpFunctionalExtensions;
using MediatR;

namespace TeamFlow.Application.Features.Search.ListSavedFilters;

public sealed record ListSavedFiltersQuery(
    Guid ProjectId
) : IRequest<Result<IReadOnlyList<SavedFilterDto>>>;
