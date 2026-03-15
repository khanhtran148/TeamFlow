using CSharpFunctionalExtensions;
using MediatR;
using TeamFlow.Application.Common.Models;
using TeamFlow.Application.Features.Releases;

namespace TeamFlow.Application.Features.Releases.ListReleases;

public sealed record ListReleasesQuery(
    Guid ProjectId,
    int Page,
    int PageSize
) : IRequest<Result<PagedResult<ReleaseDto>>>;
